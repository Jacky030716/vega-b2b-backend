using System.Text;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.Documents;
using CleanArc.Application.Contracts.Infrastructure.Rag;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Games.Commands;

internal sealed class GenerateAiChallengeDraftCommandHandler(
  IUnitOfWork unitOfWork,
  IChallengeDocumentExtractor challengeDocumentExtractor,
  IRagRetrievalService ragRetrievalService,
  IAiGenerationService aiGenerationService,
  ILogger<GenerateAiChallengeDraftCommandHandler> logger)
  : IRequestHandler<GenerateAiChallengeDraftCommand, OperationResult<GenerateAiChallengeDraftDto>>
{
  private static readonly JsonSerializerOptions _jsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
  };

  private static readonly JsonSerializerOptions _camelCase = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  public async ValueTask<OperationResult<GenerateAiChallengeDraftDto>> Handle(
    GenerateAiChallengeDraftCommand request,
    CancellationToken cancellationToken)
  {
    if (!IsSupportedGameKey(request.GameKey))
      return OperationResult<GenerateAiChallengeDraftDto>.FailureResult("Unsupported game key for AI generation.");

    var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
    if (classroom is null || !classroom.IsActive)
      return OperationResult<GenerateAiChallengeDraftDto>.NotFoundResult("Classroom not found.");

    if (classroom.TeacherId != request.UserId)
      return OperationResult<GenerateAiChallengeDraftDto>.UnauthorizedResult("You are not authorized to generate challenges for this classroom.");

    var prompt = request.Prompt?.Trim() ?? string.Empty;
    string? extractedDocumentText = null;
    string? sourceDocumentName = null;

    if (request.SourceDocument is not null)
    {
      sourceDocumentName = request.SourceDocument.FileName?.Trim();
      var extraction = await challengeDocumentExtractor.ExtractTextAsync(request.SourceDocument, cancellationToken);
      if (!extraction.IsSuccess)
        return OperationResult<GenerateAiChallengeDraftDto>.FailureResult(extraction.ErrorMessage ?? "Unable to read syllabus document.");

      extractedDocumentText = extraction.Result?.Trim();
    }

    if (string.IsNullOrWhiteSpace(prompt) && string.IsNullOrWhiteSpace(extractedDocumentText))
      return OperationResult<GenerateAiChallengeDraftDto>.FailureResult("Provide a prompt or upload a syllabus document.");

    var retrieval = await ragRetrievalService.BuildAugmentedContextAsync(
      new RagRetrievalRequest(prompt, sourceDocumentName, extractedDocumentText),
      cancellationToken);

    if (!retrieval.IsSuccess)
      return OperationResult<GenerateAiChallengeDraftDto>.FailureResult(retrieval.ErrorMessage ?? "RAG retrieval failed.");

    var systemPrompt = BuildSystemPrompt(request.GameKey);
    var userPrompt = BuildUserPrompt(request.GameKey, prompt, retrieval.Result.AugmentedContext);

    Console.WriteLine($"[DEBUG] Generating AI Challenge for: {request.GameKey}");
    logger.LogInformation("Sending prompt to AI for {GameKey}. System Prompt: {SystemPrompt}", request.GameKey, systemPrompt);
    logger.LogInformation("User Prompt: {UserPrompt}", userPrompt);

    var draftResult = await InferValidDraftAsync(
      request.GameKey,
      prompt,
      systemPrompt,
      userPrompt,
      cancellationToken);

    if (!draftResult.IsSuccess)
      return OperationResult<GenerateAiChallengeDraftDto>.FailureResult(draftResult.ErrorMessage ?? "AI generation failed.");

    var draft = draftResult.Result;

    var response = new GenerateAiChallengeDraftDto(
      draft.Title,
      draft.Description,
      draft.DraftSchema,
      draft.DraftPayload,
      draft.PlayableContentData,
      sourceDocumentName,
      retrieval.Result.RetrievedChunks
        .Select(chunk => new RagChunkDto(chunk.SourceLabel, chunk.Content, Math.Round(chunk.Similarity, 4)))
        .ToList());

    return OperationResult<GenerateAiChallengeDraftDto>.SuccessResult(response);
  }

  private async Task<OperationResult<ValidatedDraft>> InferValidDraftAsync(
    string gameKey,
    string prompt,
    string systemPrompt,
    string userPrompt,
    CancellationToken cancellationToken)
  {
    var generation = await aiGenerationService.GenerateJsonAsync(
      new ChallengeGenerationRequest(
        Model: string.Empty,
        SystemPrompt: systemPrompt,
        UserPrompt: userPrompt,
        Temperature: 0.2d,
        JsonMode: true),
      cancellationToken);

    if (!generation.IsSuccess)
    {
      logger.LogError("AI generation failed: {Error}", generation.ErrorMessage);
      return OperationResult<ValidatedDraft>.FailureResult(generation.ErrorMessage ?? "AI generation call failed.");
    }

    Console.WriteLine($"[DEBUG] Raw AI Response Length: {generation.Result.RawResponse?.Length ?? 0}");
    logger.LogInformation("Raw AI Response: {RawResponse}", generation.Result.RawResponse);

    var sanitized = SanitizeJson(generation.Result.RawResponse);
    Console.WriteLine($"[DEBUG] Sanitized JSON: {sanitized}");
    logger.LogInformation("Sanitized JSON: {SanitizedJson}", sanitized);

    var parseResult = ValidateAndMapDraft(gameKey, prompt, sanitized);
    if (parseResult.IsSuccess)
    {
      Console.WriteLine("[DEBUG] Validation Success!");
      return parseResult;
    }

    // Dump to temp file for post-mortem inspection if validation fails
    try
    {
      var logPath = Path.Combine(Path.GetTempPath(), $"vega-ai-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt");
      var logContent = $"=== RAW ===\n{generation.Result.RawResponse}\n=== SANITIZED ===\n{sanitized}\n=== PARSE ERROR ===\n{parseResult.ErrorMessage}";
      File.WriteAllText(logPath, logContent);
      Console.WriteLine($"[DEBUG] AI response written to: {logPath}");
    }
    catch { /* never break on logging */ }

    logger.LogWarning("Draft validation failed: {Error}", parseResult.ErrorMessage);
    return OperationResult<ValidatedDraft>.FailureResult(parseResult.ErrorMessage ?? "AI returned an invalid draft structure.");
  }

  private static OperationResult<ValidatedDraft> ValidateAndMapDraft(
    string gameKey,
    string originalPrompt,
    string rawJson)
  {
    JsonElement rootElement;
    try
    {
      using var doc = JsonDocument.Parse(rawJson);
      rootElement = doc.RootElement.Clone();
    }
    catch (Exception)
    {
      return OperationResult<ValidatedDraft>.FailureResult("AI returned malformed JSON draft.");
    }

    // Strategy 1: Standard envelope { title, description, content: { ... } }
    // Strategy 2: Flat root treated as content directly (Gemma often skips the wrapper)
    // Strategy 3: "content" is a JSON string that needs a second parse

    // Resolve title + description from root (optional)
    var title = rootElement.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String
      ? titleEl.GetString()?.Trim()
      : null;
    var description = rootElement.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
      ? descEl.GetString()?.Trim()
      : null;

    var normalizedTitle = string.IsNullOrWhiteSpace(title)
      ? BuildFallbackTitle(gameKey, originalPrompt)
      : title!;

    var normalizedDescription = string.IsNullOrWhiteSpace(description)
      ? "AI-generated classroom challenge draft."
      : description!;

    // Resolve content element using the three strategies
    JsonElement contentElement;

    if (rootElement.TryGetProperty("content", out var contentProp))
    {
      if (contentProp.ValueKind == JsonValueKind.String)
      {
        // Strategy 3: "content" is a JSON string — parse it
        try
        {
          using var innerDoc = JsonDocument.Parse(contentProp.GetString() ?? "{}");
          contentElement = innerDoc.RootElement.Clone();
        }
        catch
        {
          return OperationResult<ValidatedDraft>.FailureResult("AI 'content' field contains invalid JSON string.");
        }
      }
      else if (contentProp.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
      {
        // Strategy 1: Standard nested content object
        contentElement = contentProp;
      }
      else
      {
        return OperationResult<ValidatedDraft>.FailureResult("AI response 'content' field is null or missing.");
      }
    }
    else
    {
      // Strategy 2: No "content" key — treat the whole root as the content payload
      contentElement = rootElement;
    }

    return gameKey switch
    {
      "magic_backpack" => ValidateMagicBackpack(normalizedTitle, normalizedDescription, contentElement),
      "word_pair" => ValidateWordPair(normalizedTitle, normalizedDescription, contentElement),
      "word_bridge" => ValidateWordBuilder(normalizedTitle, normalizedDescription, contentElement),
      _ => OperationResult<ValidatedDraft>.FailureResult("Unsupported game key for draft validation."),
    };
  }

  private static OperationResult<ValidatedDraft> ValidateMagicBackpack(
    string title,
    string description,
    JsonElement contentElement)
  {
    // Try multiple field names — Gemma often uses 'vocabulary', 'words', or 'elements' instead of 'items'
    List<string> items = new();
    foreach (var fieldName in new[] { "items", "vocabulary", "words", "elements", "sequence" })
    {
      if (contentElement.TryGetProperty(fieldName, out var arr) && arr.ValueKind == JsonValueKind.Array)
      {
        items = arr.EnumerateArray()
          .Where(el => el.ValueKind == JsonValueKind.String)
          .Select(el => el.GetString()?.Trim() ?? string.Empty)
          .Where(s => s.Length > 0)
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .ToList();
        if (items.Count > 0) break;
      }
    }

    if (items.Count < 3)
      return OperationResult<ValidatedDraft>.FailureResult(
        $"Magic Backpack requires at least 3 items (found {items.Count}). Tried fields: items/vocabulary/words/elements/sequence.");

    var theme = contentElement.TryGetProperty("theme", out var themeEl) && themeEl.ValueKind == JsonValueKind.String
      ? themeEl.GetString()?.Trim() ?? "custom"
      : "custom";

    var sequenceLength = contentElement.TryGetProperty("sequenceLength", out var seqEl) && seqEl.TryGetInt32(out var seqVal) && seqVal > 0
      ? Math.Min(seqVal, items.Count)
      : items.Count;

    var draftPayload = JsonSerializer.Serialize(new { items, theme, sequenceLength }, _camelCase);
    var playableContentData = JsonSerializer.Serialize(new
    {
      Theme = theme,
      SequenceLength = sequenceLength,
      GhostMode = false,
      Items = items,
    });

    return OperationResult<ValidatedDraft>.SuccessResult(
      new ValidatedDraft(title, description, "magic_backpack", draftPayload, playableContentData));
  }


  private static OperationResult<ValidatedDraft> ValidateWordPair(
    string title,
    string description,
    JsonElement contentElement)
  {
    WordPairDraftContent? content;
    try
    {
      content = JsonSerializer.Deserialize<WordPairDraftContent>(contentElement.GetRawText(), _jsonOptions);
    }
    catch
    {
      return OperationResult<ValidatedDraft>.FailureResult("Word Pair draft schema is invalid.");
    }

    var pairs = (content?.Pairs ?? new List<WordPairDraftItem>())
      .Select(pair => new WordPairDraftItem
      {
        Key = pair.Key?.Trim() ?? string.Empty,
        Value = pair.Value?.Trim() ?? string.Empty,
      })
      .Where(pair => pair.Key.Length > 0 && pair.Value.Length > 0)
      .DistinctBy(pair => $"{pair.Key}|{pair.Value}")
      .ToList();

    if (pairs.Count < 3)
      return OperationResult<ValidatedDraft>.FailureResult("Word Pair requires at least 3 pairs.");

    var draftPayload = JsonSerializer.Serialize(new
    {
      pairs = pairs.Select(pair => new { key = pair.Key, value = pair.Value }).ToList(),
      isBilingual = content?.IsBilingual ?? true,
    }, _camelCase);

    var playableContentData = JsonSerializer.Serialize(new
    {
      Pairs = pairs.Select(pair => new
      {
        Word = pair.Key,
        Translation = pair.Value,
        ImageRef = (string?)null,
        ImageKey = (string?)null,
      }).ToList(),
    });

    return OperationResult<ValidatedDraft>.SuccessResult(
      new ValidatedDraft(title, description, "word_pair", draftPayload, playableContentData));
  }

  private static OperationResult<ValidatedDraft> ValidateWordBuilder(
    string title,
    string description,
    JsonElement contentElement)
  {
    WordBuilderDraftContent? content;
    try
    {
      content = JsonSerializer.Deserialize<WordBuilderDraftContent>(contentElement.GetRawText(), _jsonOptions);
    }
    catch
    {
      return OperationResult<ValidatedDraft>.FailureResult("Word Builder draft schema is invalid.");
    }

    var words = (content?.Words ?? new List<string>())
      .Select(word => word.Trim())
      .Where(word => word.Length > 0)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();

    if (words.Count < 3)
      return OperationResult<ValidatedDraft>.FailureResult("Word Builder requires at least 3 words.");

    var hints = (content?.Hints ?? new List<string>())
      .Select(hint => hint.Trim())
      .Where(hint => hint.Length > 0)
      .ToList();

    while (hints.Count < words.Count)
    {
      var nextWord = words[hints.Count];
      hints.Add($"Hint: {nextWord}");
    }

    var draftPayload = JsonSerializer.Serialize(new
    {
      words,
      hints,
    }, _camelCase);

    var playableContentData = JsonSerializer.Serialize(new
    {
      Words = words.Select((word, index) => new
      {
        Target = word.ToUpperInvariant(),
        Translation = hints[index],
        Difficulty = "medium",
        ImageRef = (string?)null,
      }).ToList(),
    });

    return OperationResult<ValidatedDraft>.SuccessResult(
      new ValidatedDraft(title, description, "word_builder", draftPayload, playableContentData));
  }

  private static string BuildSystemPrompt(string gameKey)
  {
    return gameKey switch
    {
      "magic_backpack" => """
SYSTEM: You are a structural data converter. Use [CONTEXT] to populate [SCHEMA].
OUTPUT: PURE JSON ONLY. NO MARKDOWN. NO COMMENTS. NO CHAT.

[SCHEMA]
{
  "title": "string",
  "description": "string",
  "content": {
    "items": ["string", "string", "string"],
    "theme": "string",
    "sequenceLength": 3
  }
}

[RULES]
1. Start with { and end with }.
2. "items" must have >= 3 elements.
""",
      "word_pair" => """
SYSTEM: You are a structural data converter. Use [CONTEXT] to populate [SCHEMA].
OUTPUT: PURE JSON ONLY. NO MARKDOWN. NO COMMENTS. NO CHAT.

[SCHEMA]
{
  "title": "string",
  "description": "string",
  "content": {
    "pairs": [{ "key": "string", "value": "string" }],
    "isBilingual": true
  }
}

[RULES]
1. Start with { and end with }.
2. "pairs" must have >= 3 elements.
""",
      _ => """
SYSTEM: You are a structural data converter. Use [CONTEXT] to populate [SCHEMA].
OUTPUT: PURE JSON ONLY. NO MARKDOWN. NO COMMENTS. NO CHAT.

[SCHEMA]
{
  "title": "string",
  "description": "string",
  "content": {
    "words": ["string", "string", "string"],
    "hints": ["string", "string", "string"]
  }
}

[RULES]
1. Start with { and end with }.
2. "words" must have >= 3 elements.
""",
    };
  }

  private static string BuildUserPrompt(string gameKey, string prompt, string context)
  {
    var readableGameName = gameKey switch
    {
      "magic_backpack" => "Magic Backpack",
      "word_pair" => "Word Pair",
      _ => "Word Builder",
    };

    var sb = new StringBuilder();
    sb.AppendLine("[CONTEXT]");
    sb.AppendLine(string.IsNullOrWhiteSpace(context) ? "No retrieval context was found." : context);
    sb.AppendLine("[/CONTEXT]");
    sb.AppendLine();
    sb.AppendLine("[REQUEST]");
    sb.AppendLine($"Game Type: {readableGameName}");
    sb.AppendLine($"Teacher Prompt: {prompt}");
    sb.AppendLine("[/REQUEST]");
    sb.AppendLine();
    sb.AppendLine("OUTPUT RULES: PURE JSON ONLY. NO MARKDOWN CODE FENCES. NO CONVERSATION.");
    return sb.ToString();
  }

  private static string BuildFallbackTitle(string gameKey, string prompt)
  {
    var prefix = gameKey switch
    {
      "magic_backpack" => "Magic Backpack",
      "word_pair" => "Word Pair",
      _ => "Word Builder",
    };

    if (string.IsNullOrWhiteSpace(prompt))
      return $"{prefix} Challenge";

    var trimmed = prompt.Trim();
    var titleSeed = trimmed.Length > 42 ? trimmed[..42].Trim() : trimmed;
    return $"{prefix}: {titleSeed}";
  }

  private static string SanitizeJson(string rawResponse)
  {
    if (string.IsNullOrWhiteSpace(rawResponse)) return string.Empty;

    var trimmed = rawResponse.Trim();

    // 1. Remove markdown code fences if present (```json ... ``` or ``` ... ```)
    if (trimmed.Contains("```"))
    {
      // Try to extract content between the first and last triple backticks
      var firstIndex = trimmed.IndexOf("```", StringComparison.Ordinal);
      var lastIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
      
      if (firstIndex != -1 && lastIndex != -1 && firstIndex != lastIndex)
      {
        var content = trimmed.Substring(firstIndex + 3, lastIndex - (firstIndex + 3)).Trim();
        // Remove language identifier if present (e.g., "json\n")
        if (content.StartsWith("json", StringComparison.OrdinalIgnoreCase))
        {
          content = content.Substring(4).Trim();
        }
        return content;
      }
    }

    return trimmed;
  }

  private static bool IsSupportedGameKey(string gameKey)
    => gameKey is "magic_backpack" or "word_pair" or "word_bridge";

  private static bool IsTransientGenerationFailure(string? errorMessage)
  {
    if (string.IsNullOrWhiteSpace(errorMessage))
      return false;

    return errorMessage.Contains("(429)", StringComparison.Ordinal)
           || errorMessage.Contains("(500)", StringComparison.Ordinal)
           || errorMessage.Contains("(502)", StringComparison.Ordinal)
           || errorMessage.Contains("(503)", StringComparison.Ordinal)
           || errorMessage.Contains("(504)", StringComparison.Ordinal)
           || errorMessage.Contains("timed out", StringComparison.OrdinalIgnoreCase)
           || errorMessage.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase);
  }

  private sealed record ValidatedDraft(
    string Title,
    string Description,
    string DraftSchema,
    string DraftPayload,
    string PlayableContentData);



  private sealed class WordPairDraftContent
  {
    public List<WordPairDraftItem> Pairs { get; init; } = new();
    public bool? IsBilingual { get; init; }
  }

  private sealed class WordPairDraftItem
  {
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
  }

  private sealed class WordBuilderDraftContent
  {
    public List<string> Words { get; init; } = new();
    public List<string> Hints { get; init; } = new();
  }
}
