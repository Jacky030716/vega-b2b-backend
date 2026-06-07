using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#nullable enable

namespace CleanArc.Infrastructure.Persistence.Services.AI;

public sealed class ChallengeAiPipelineService(
  IAiGenerationService aiGenerationService,
  IAiPromptRegistry promptRegistry,
  IAiAuditService aiAuditService,
  IAiUsageService aiUsageService,
  IAiRateLimitService aiRateLimitService,
  IOptions<GoogleAiOptions> googleAiOptions,
  ILogger<ChallengeAiPipelineService> logger) : IChallengeAiPipelineService
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private static readonly JsonSerializerOptions ReadOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };

  public async Task<OperationResult<CustomVocabularyGenerationResult>> GenerateStructuredVocabularyFromInputAsync(
    CustomVocabularyGenerationRequest request,
    CancellationToken cancellationToken)
  {
    if (request.RelatedUserId is int userId && request.ExistingAuditLogId is null)
    {
      var access = await EnsureAccessAsync(userId, AiFeatureTypes.CustomChallengeGeneration, cancellationToken);
      if (!access.IsSuccess)
        return OperationResult<CustomVocabularyGenerationResult>.FailureResult(access.ErrorMessage ?? "AI request blocked.");
    }


    var prompt = promptRegistry.Get(AiUseCases.CustomChallengeExtraction, request.GameKey);
    var systemPrompt = prompt.SystemInstruction;
    var userPrompt = BuildCustomExtractionUserPrompt(request.GameKey, request.Prompt, request.AugmentedContext);
    var auditLogId = request.ExistingAuditLogId ?? await StartAuditAsync(
      AiUseCases.CustomChallengeExtraction,
      prompt,
      new
      {
        request.GameKey,
        request.Prompt,
        request.AugmentedContext
      },
      request.RelatedUserId,
      request.RelatedClassroomId,
      null,
      cancellationToken);


    var generation = await GenerateJsonAsync(AiUseCases.CustomChallengeExtraction, systemPrompt, userPrompt, 0.2d, cancellationToken);
    if (!generation.IsSuccess)
    {
      await aiAuditService.FailAsync(auditLogId, null, new[] { generation.ErrorMessage ?? "AI generation failed." }, cancellationToken);
      return OperationResult<CustomVocabularyGenerationResult>.FailureResult(generation.ErrorMessage ?? "AI generation failed.");
    }

    var sanitized = SanitizeJson(generation.Result.RawResponse);

    var draft = ValidateAndMapCustomDraft(request.GameKey, request.Prompt, sanitized);
    LogValidation(AiUseCases.CustomChallengeExtraction, draft.IsSuccess, draft.ErrorMessage);

    if (!draft.IsSuccess)
    {
      await aiAuditService.CompleteAsync(
        auditLogId,
        sanitized,
        "{}",
        AiValidationStatuses.Invalid,
        new[] { draft.ErrorMessage ?? "AI returned an invalid draft structure." },
        cancellationToken);
      return OperationResult<CustomVocabularyGenerationResult>.FailureResult(draft.ErrorMessage ?? "AI returned an invalid draft structure.");
    }

    var result = draft.Result with { AiAuditLogId = auditLogId };
    await aiAuditService.CompleteAsync(
      auditLogId,
      sanitized,
      JsonSerializer.Serialize(result, JsonOptions),
      AiValidationStatuses.Valid,
      Array.Empty<string>(),
      cancellationToken);

    if (request.RelatedUserId is int successUserId)
    {
      await ConsumeUsageAsync(
        successUserId,
        AiFeatureTypes.CustomChallengeGeneration,
        AiUseCases.CustomChallengeExtraction,
        request.RelatedClassroomId,
        null,
        cancellationToken);
    }

    return OperationResult<CustomVocabularyGenerationResult>.SuccessResult(result);
  }

  public async Task<OperationResult<ModuleChallengePlanResult>> GenerateModuleChallengePlanAsync(
    ModuleChallengePlanRequest request,
    CancellationToken cancellationToken)
  {
    if (request.RelatedUserId is int userId)
    {
      var access = await EnsureAccessAsync(userId, ResolveModulePlanFeatureType(request), cancellationToken);
      if (!access.IsSuccess)
        return OperationResult<ModuleChallengePlanResult>.FailureResult(access.ErrorMessage ?? "AI request blocked.", EmptyModulePlanResult(0));
    }

    if (request.Items.Count == 0)
      return OperationResult<ModuleChallengePlanResult>.FailureResult("Module has no vocabulary items.");

    var prompt = promptRegistry.Get(AiUseCases.ModuleChallengePlanning);
    var systemPrompt = prompt.SystemInstruction;
    var userPrompt = BuildModulePlanUserPrompt(request);
    var auditLogId = await StartAuditAsync(
      AiUseCases.ModuleChallengePlanning,
      prompt,
      request,
      request.RelatedUserId,
      request.RelatedClassroomId,
      request.ModuleId,
      cancellationToken);

    var generation = await GenerateJsonAsync(AiUseCases.ModuleChallengePlanning, systemPrompt, userPrompt, 0.15d, cancellationToken);
    if (!generation.IsSuccess)
    {
      await aiAuditService.FailAsync(auditLogId, null, new[] { generation.ErrorMessage ?? "AI module planning failed." }, cancellationToken);
      return OperationResult<ModuleChallengePlanResult>.FailureResult(
        generation.ErrorMessage ?? "AI module planning failed.",
        EmptyModulePlanResult(auditLogId));
    }

    var sanitized = SanitizeJson(generation.Result.RawResponse);

    var validation = ValidateModulePlan(sanitized, request);
    LogValidation(AiUseCases.ModuleChallengePlanning, validation.IsSuccess, validation.ErrorMessage);

    if (!validation.IsSuccess)
    {
      await aiAuditService.CompleteAsync(
        auditLogId,
        sanitized,
        "{}",
        AiValidationStatuses.Invalid,
        new[] { validation.ErrorMessage ?? "AI module plan is invalid." },
        cancellationToken);
      return OperationResult<ModuleChallengePlanResult>.FailureResult(
        validation.ErrorMessage ?? "AI module plan is invalid.",
        EmptyModulePlanResult(auditLogId));
    }

    var result = validation.Result with { AiAuditLogId = auditLogId };
    await aiAuditService.CompleteAsync(
      auditLogId,
      sanitized,
      JsonSerializer.Serialize(result, JsonOptions),
      AiValidationStatuses.Valid,
      Array.Empty<string>(),
      cancellationToken);

    if (request.RelatedUserId is int successUserId)
    {
      await ConsumeUsageAsync(
        successUserId,
        ResolveModulePlanFeatureType(request),
        AiUseCases.ModuleChallengePlanning,
        request.RelatedClassroomId,
        request.ModuleId,
        cancellationToken);
    }

    return OperationResult<ModuleChallengePlanResult>.SuccessResult(result);
  }

  public async Task<OperationResult<GeneratedAdaptiveChallengePreviewDto>> GenerateGameConfigAsync(
    GameConfigGenerationRequest request,
    CancellationToken cancellationToken)
  {
    if (request.Words.Count == 0)
      return OperationResult<GeneratedAdaptiveChallengePreviewDto>.FailureResult("Game config requires at least one word.");

    var gameType = NormalizeGameType(request.GameType);
    if (!IsAdaptiveGameType(gameType))
      return OperationResult<GeneratedAdaptiveChallengePreviewDto>.FailureResult("Invalid adaptive game type.");

    var useCase = ToGameConfigUseCase(gameType);
    var prompt = promptRegistry.Get(useCase);
    var auditLogId = await StartAuditAsync(
      useCase,
      prompt,
      request with { GameType = gameType },
      null,
      request.ClassroomId,
      request.ModuleId,
      cancellationToken,
      provider: "RULE_BASED",
      modelName: null);

    try
    {
      var preview = BuildAdaptivePreview(request with { GameType = gameType });
      await aiAuditService.CompleteAsync(
        auditLogId,
        JsonSerializer.Serialize(new { gameType, config = "rule_based" }, JsonOptions),
        JsonSerializer.Serialize(new { gameType, config = "rule_based" }, JsonOptions),
        AiValidationStatuses.Valid,
        Array.Empty<string>(),
        cancellationToken);
      return OperationResult<GeneratedAdaptiveChallengePreviewDto>.SuccessResult(preview);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Validated AI config could not be mapped to adaptive preview for {GameType}.", gameType);
      await aiAuditService.FailAsync(auditLogId, null, new[] { ex.Message }, cancellationToken);
      return OperationResult<GeneratedAdaptiveChallengePreviewDto>.FailureResult(ex.Message);
    }
  }

  private async Task<int> StartAuditAsync(
    string useCase,
    AiPromptDefinition prompt,
    object inputPayload,
    int? relatedUserId,
    int? relatedClassroomId,
    int? relatedModuleId,
    CancellationToken cancellationToken,
    string provider = "GEMINI",
    string? modelName = null)
  {
    var model = modelName ?? googleAiOptions.Value.ModelId;
    return await aiAuditService.StartAsync(
      new AiAuditStartRequest(
        useCase,
        provider,
        model,
        prompt.Version,
        JsonSerializer.Serialize(inputPayload, JsonOptions),
        relatedUserId,
        relatedClassroomId,
        relatedModuleId),
      cancellationToken);
  }

  private async Task<OperationResult<bool>> EnsureAccessAsync(int userId, string featureType, CancellationToken cancellationToken)
  {
    var rateLimit = await aiRateLimitService.TryAcquireAsync(userId, featureType, cancellationToken);
    if (!rateLimit.Allowed)
      return OperationResult<bool>.FailureResult("Too many AI requests. Please try again later.");

    var quota = await aiUsageService.GetRemainingQuotaAsync(userId, featureType, cancellationToken);
    if (quota.Remaining <= 0)
      return OperationResult<bool>.FailureResult("Your AI quota is exhausted for this month.");

    return OperationResult<bool>.SuccessResult(true);
  }

  private static ModuleChallengePlanResult EmptyModulePlanResult(int auditLogId)
    => new(Array.Empty<string>(), string.Empty, 1, string.Empty, string.Empty, auditLogId);

  private async Task<OperationResult<ChallengeGenerationResult>> GenerateJsonAsync(
    string useCase,
    string systemPrompt,
    string userPrompt,
    double temperature,
    CancellationToken cancellationToken)
  {
    var result = await aiGenerationService.GenerateJsonAsync(
      new ChallengeGenerationRequest(
        Model: "gemini-3.5-flash",
        SystemPrompt: systemPrompt,
        UserPrompt: userPrompt,
        Temperature: temperature,
        JsonMode: true),
      cancellationToken);

    if (!result.IsSuccess)
      logger.LogError("AI error for {UseCase}: {Error}", useCase, result.ErrorMessage);

    return result;
  }

  private static OperationResult<ModuleChallengePlanResult> ValidateModulePlan(
    string rawJson,
    ModuleChallengePlanRequest request)
  {
    ModuleChallengePlanResponse? response;
    try
    {
      response = JsonSerializer.Deserialize<ModuleChallengePlanResponse>(rawJson, ReadOptions);
    }
    catch
    {
      return OperationResult<ModuleChallengePlanResult>.FailureResult("AI returned malformed module plan JSON.");
    }

    if (response is null)
      return OperationResult<ModuleChallengePlanResult>.FailureResult("AI returned an empty module plan.");

    var gameType = NormalizeGameType(response.RecommendedGameType);
    if (!IsAdaptiveGameType(gameType))
      return OperationResult<ModuleChallengePlanResult>.FailureResult("AI returned an invalid game type.");

    var difficulty = response.DifficultyLevel;
    if (difficulty is < 1 or > 3)
      return OperationResult<ModuleChallengePlanResult>.FailureResult("AI returned an invalid difficulty level.");

    if (string.IsNullOrWhiteSpace(response.Reason))
      return OperationResult<ModuleChallengePlanResult>.FailureResult("AI module plan is missing reason.");

    if (string.IsNullOrWhiteSpace(response.FocusType))
      return OperationResult<ModuleChallengePlanResult>.FailureResult("AI module plan is missing focusType.");

    var moduleWords = request.Items
      .SelectMany(item => new[] { item.Word, item.BmText, item.EnText, item.ZhText })
      .Where(value => !string.IsNullOrWhiteSpace(value))
      .Select(value => value!.Trim())
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var selectedWords = (response.SelectedWords ?? new List<string>())
      .Select(word => word.Trim())
      .Where(word => word.Length > 0)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();

    if (selectedWords.Count == 0)
      return OperationResult<ModuleChallengePlanResult>.FailureResult("AI did not select any words.");

    var hallucinated = selectedWords.Where(word => !moduleWords.Contains(word)).ToList();
    if (hallucinated.Count > 0)
      return OperationResult<ModuleChallengePlanResult>.FailureResult($"AI selected words outside the module: {string.Join(", ", hallucinated)}");

    if (request.WeakWords.Count > 0)
    {
      var weakSet = request.WeakWords.ToHashSet(StringComparer.OrdinalIgnoreCase);
      if (!selectedWords.Any(weakSet.Contains))
        return OperationResult<ModuleChallengePlanResult>.FailureResult("AI did not prioritize available weak words.");
    }

    var focusType = NormalizeFocusType(response.FocusType, request.Mode);
    var reason = response.Reason.Trim();

    return OperationResult<ModuleChallengePlanResult>.SuccessResult(
      new ModuleChallengePlanResult(selectedWords, gameType, difficulty, reason, focusType));
  }

  private static GeneratedAdaptiveChallengePreviewDto BuildAdaptivePreview(GameConfigGenerationRequest request)
  {
    var category = ChallengeGenerator.ToCategory(request.GameType);
    var gameKey = ChallengeGenerator.ToGameKey(request.GameType);
    var titleBase = string.IsNullOrWhiteSpace(request.ModuleTitle) ? request.Mode.Replace('_', ' ') : request.ModuleTitle;
    var items = request.Words.Select(item => item with
    {
      DifficultyLevel = Math.Clamp(request.DifficultyLevel, 1, 3)
    }).ToList();

    List<SyllableSushiSpecDto>? syllableSushiSpecs = null;
    SyllableSushiSpecDto? primarySyllableSushiSpec = null;
    List<SpellCatcherSpecDto>? spellCatcherSpecs = null;
    SpellCatcherSpecDto? primarySpellCatcherSpec = null;

    if (request.GameType == "SPELL_CATCHER")
    {
      spellCatcherSpecs = items
        .Select(item => ChallengeGenerator.BuildSpellCatcherSpec(
          item,
          new SpellCatcherWeakness(true, false, true)))
        .ToList();
      primarySpellCatcherSpec = spellCatcherSpecs.FirstOrDefault();
      items = items.Zip(spellCatcherSpecs, (item, spec) => item with
      {
        DifficultyLevel = spec.DifficultyLevel,
        SpellCatcherSpecJson = JsonSerializer.Serialize(spec, ChallengeGenerator.JsonOptions)
      }).ToList();
    }

    if (request.GameType == "SYLLABLE_SUSHI")
    {
      syllableSushiSpecs = items.Select(ChallengeGenerator.BuildSyllableSushiSpec).ToList();
      primarySyllableSushiSpec = syllableSushiSpecs.FirstOrDefault();
      items = items.Zip(syllableSushiSpecs, (item, spec) => item with
      {
        SyllablePoolJson = JsonSerializer.Serialize(spec.SyllablePool, ChallengeGenerator.JsonOptions),
        DistractorsJson = JsonSerializer.Serialize(spec.Distractors, ChallengeGenerator.JsonOptions),
        CorrectOrderJson = JsonSerializer.Serialize(spec.CorrectOrder, ChallengeGenerator.JsonOptions),
        DifficultyLevel = spec.DifficultyLevel
      }).ToList();
    }

    var contentData = JsonSerializer.Serialize(new
    {
      gameTemplateCode = request.GameType,
      category,
      objective = request.Mode,
      moduleId = request.ModuleId,
      spellCatcherSpec = primarySpellCatcherSpec,
      spellCatcherSpecs,
      syllableSushiSpec = primarySyllableSushiSpec,
      syllableSushiSpecs,
      items = items.Select(i => new
      {
        vocabularyItemId = i.VocabularyItemId,
        word = i.Word,
        normalizedWord = i.NormalizedWord,
        hint = i.Hint,
        meaningText = i.MeaningText,
        exampleSentence = i.ExampleSentence,
        syllablesJson = ChallengeGenerator.TryParseJson(i.SyllablesJson) ?? JsonNode.Parse("[]"),
        difficultyLevel = i.DifficultyLevel,
        bmText = i.BmText,
        zhText = i.ZhText,
        enText = i.EnText,
        syllableText = i.SyllableText,
        itemType = i.ItemType,
        displayOrder = i.DisplayOrder,
        syllablePoolJson = ChallengeGenerator.TryParseJson(i.SyllablePoolJson) ?? JsonNode.Parse("[]"),
        distractorsJson = ChallengeGenerator.TryParseJson(i.DistractorsJson) ?? JsonNode.Parse("[]"),
        correctOrderJson = ChallengeGenerator.TryParseJson(i.CorrectOrderJson) ?? JsonNode.Parse("[]"),
        spellCatcherSpecJson = ChallengeGenerator.TryParseJson(i.SpellCatcherSpecJson),
        language = i.Language
      })
    }, ChallengeGenerator.JsonOptions);

    var title = request.GameType switch
    {
      "SPELL_CATCHER" => $"Spell Catcher: {titleBase}",
      "SYLLABLE_SUSHI" => $"Syllable Sushi: {titleBase}",
      "VOICE_BRIDGE" => $"Voice Bridge: {titleBase}",
      _ => $"Adaptive Practice: {titleBase}"
    };

    return new GeneratedAdaptiveChallengePreviewDto(
      title,
      $"AI-planned {category.ToLowerInvariant()} practice generated from {request.SourceType}.",
      request.GameType,
      gameKey,
      category,
      Math.Clamp(request.DifficultyLevel, 1, 3),
      request.ModuleId,
      null,
      request.ClassroomId,
      request.Mode,
      request.SourceType,
      contentData,
      JsonSerializer.Serialize(new
      {
        targetType = "class",
        objective = request.Mode,
        sourceType = request.SourceType,
        learningFocus = request.Mode.Replace('_', ' '),
        configPipeline = "rule_based"
      }, ChallengeGenerator.JsonOptions),
      items,
      primarySyllableSushiSpec,
      primarySpellCatcherSpec);
  }

  private static OperationResult<CustomVocabularyGenerationResult> ValidateAndMapCustomDraft(
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
    catch
    {
      return OperationResult<CustomVocabularyGenerationResult>.FailureResult("AI returned malformed JSON draft.");
    }

    var title = rootElement.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String
      ? titleEl.GetString()?.Trim()
      : null;
    var description = rootElement.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
      ? descEl.GetString()?.Trim()
      : null;

    var normalizedTitle = string.IsNullOrWhiteSpace(title) ? BuildFallbackTitle(gameKey, originalPrompt) : title!;
    var normalizedDescription = string.IsNullOrWhiteSpace(description) ? "AI-generated classroom challenge draft." : description!;

    JsonElement contentElement;
    if (rootElement.TryGetProperty("content", out var contentProp))
    {
      if (contentProp.ValueKind == JsonValueKind.String)
      {
        try
        {
          using var innerDoc = JsonDocument.Parse(contentProp.GetString() ?? "{}");
          contentElement = innerDoc.RootElement.Clone();
        }
        catch
        {
          return OperationResult<CustomVocabularyGenerationResult>.FailureResult("AI 'content' field contains invalid JSON string.");
        }
      }
      else if (contentProp.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
      {
        contentElement = contentProp;
      }
      else
      {
        return OperationResult<CustomVocabularyGenerationResult>.FailureResult("AI response 'content' field is null or missing.");
      }
    }
    else
    {
      contentElement = rootElement;
    }

    var items = new List<JsonElement>();
    if (contentElement.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
    {
      items = itemsElement.EnumerateArray()
        .Where(item => item.ValueKind == JsonValueKind.Object)
        .ToList();
    }

    if (items.Count < 3)
      return OperationResult<CustomVocabularyGenerationResult>.FailureResult("AI draft requires at least 3 items.");

    var gameTemplateCode = gameKey switch
    {
      "spell_catcher" => "SPELL_CATCHER",
      "syllable_sushi" => "SYLLABLE_SUSHI",
      "voice_bridge" => "VOICE_BRIDGE",
      _ => gameKey.ToUpperInvariant()
    };

    var normalizedContentJson = contentElement.GetRawText();
    var draftPayload = JsonSerializer.Serialize(new
    {
      gameTemplateCode,
      content = JsonNode.Parse(normalizedContentJson),
      items = items.Select(item => JsonNode.Parse(item.GetRawText()))
    }, JsonOptions);

    return OperationResult<CustomVocabularyGenerationResult>.SuccessResult(
      new CustomVocabularyGenerationResult(
        normalizedTitle,
        normalizedDescription,
        gameTemplateCode,
        draftPayload,
        normalizedContentJson));
  }

  private static string BuildCustomExtractionUserPrompt(string gameKey, string prompt, string context)
  {
    var readableGameName = gameKey switch
    {
      "spell_catcher" => "Spell Catcher",
      "syllable_sushi" => "Syllable Sushi",
      "voice_bridge" => "Voice Bridge",
      "translation" => "Translation",
      _ => "Adaptive Challenge"
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
    return sb.ToString();
  }

  private static string BuildModulePlanUserPrompt(ModuleChallengePlanRequest request)
    => JsonSerializer.Serialize(new
    {
      request.ModuleId,
      request.ModuleTitle,
      request.Subject,
      request.YearLevel,
      requestedGameType = request.RequestedGameType,
      mode = request.Mode,
      studentWeakness = new
      {
        weakWords = request.WeakWords,
        weakSkill = request.WeakSkill
      },
      items = request.Items.Select(item => new
      {
        item.VocabularyItemId,
        item.Word,
        item.BmText,
        item.EnText,
        item.ZhText,
        item.SyllablesJson,
        item.SyllableText,
        item.ItemType,
        item.DifficultyLevel,
        item.MeaningText,
        item.ExampleSentence
      })
    }, JsonOptions);

  private static string SanitizeJson(string rawResponse)
  {
    if (string.IsNullOrWhiteSpace(rawResponse))
      return string.Empty;

    var trimmed = rawResponse.Trim();
    if (!trimmed.Contains("```", StringComparison.Ordinal))
      return trimmed;

    var firstIndex = trimmed.IndexOf("```", StringComparison.Ordinal);
    var lastIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
    if (firstIndex == -1 || lastIndex == -1 || firstIndex == lastIndex)
      return trimmed;

    var content = trimmed.Substring(firstIndex + 3, lastIndex - (firstIndex + 3)).Trim();
    return content.StartsWith("json", StringComparison.OrdinalIgnoreCase)
      ? content.Substring(4).Trim()
      : content;
  }

  private static string BuildFallbackTitle(string gameKey, string prompt)
  {
    var prefix = gameKey switch
    {
      "spell_catcher" => "Spell Catcher",
      "syllable_sushi" => "Syllable Sushi",
      "voice_bridge" => "Voice Bridge",
      "translation" => "Translation",
      _ => "Adaptive Challenge"
    };

    if (string.IsNullOrWhiteSpace(prompt))
      return $"{prefix} Challenge";

    var trimmed = prompt.Trim();
    var titleSeed = trimmed.Length > 42 ? trimmed[..42].Trim() : trimmed;
    return $"{prefix}: {titleSeed}";
  }

  private static bool IsAdaptiveGameType(string gameType)
    => gameType is "SPELL_CATCHER" or "SYLLABLE_SUSHI" or "VOICE_BRIDGE" or "TRANSLATION";

  private static string ToGameConfigUseCase(string gameType)
    => gameType switch
    {
      "SPELL_CATCHER" => AiUseCases.SpellCatcherConfig,
      "SYLLABLE_SUSHI" => AiUseCases.SyllableSushiConfig,
      "VOICE_BRIDGE" => AiUseCases.VoiceBridgeConfig,
      "TRANSLATION" => AiUseCases.TranslationConfig,
      _ => $"{gameType}_CONFIG"
    };

  private static string NormalizeGameType(string? gameType)
  {
    var normalized = gameType?.Trim().ToUpperInvariant() ?? string.Empty;
    return normalized switch
    {
      "SPELL_CATCHER" or "SPELLCATCHER" or "SPELL" => "SPELL_CATCHER",
      "SYLLABLE_SUSHI" or "SYLLABLESUSHI" or "SYLLABLE" => "SYLLABLE_SUSHI",
      "VOICE_BRIDGE" or "VOICEBRIDGE" or "VOICE" or "SPEAKING" => "VOICE_BRIDGE",
      "TRANSLATION" or "TRANSLATION_GAME" => "TRANSLATION",
      _ => normalized
    };
  }

  private async Task ConsumeUsageAsync(
    int userId,
    string featureType,
    string endpointKey,
    int? relatedClassroomId,
    int? relatedModuleId,
    CancellationToken cancellationToken)
  {
    await aiUsageService.ConsumeUsageAsync(
      userId,
      featureType,
      endpointKey,
      "GEMINI",
      googleAiOptions.Value.ModelId,
      1,
      true,
      null,
      relatedModuleId.HasValue ? "module" : relatedClassroomId.HasValue ? "classroom" : null,
      relatedModuleId ?? relatedClassroomId,
      cancellationToken);
  }

  private static string ResolveModulePlanFeatureType(ModuleChallengePlanRequest request)
  {
    return request.Mode.Contains("RECOVERY", StringComparison.OrdinalIgnoreCase)
      ? AiFeatureTypes.RecoveryMissionPreview
      : AiFeatureTypes.PredefinedModuleGeneration;
  }

  private static string NormalizeFocusType(string? focusType, string mode)
  {
    var normalized = focusType?.Trim().ToUpperInvariant();
    if (normalized is "WEAKNESS" or "PRACTICE" or "REVIEW")
      return normalized;

    if (mode.Contains("WEAK", StringComparison.OrdinalIgnoreCase))
      return "WEAKNESS";
    if (mode.Contains("REVIEW", StringComparison.OrdinalIgnoreCase) || mode.Contains("OVERDUE", StringComparison.OrdinalIgnoreCase))
      return "REVIEW";
    return "PRACTICE";
  }

  private void LogValidation(string useCase, bool isSuccess, string? error)
  {
    if (isSuccess)
      logger.LogInformation("AI validation passed for {UseCase}.", useCase);
    else
      logger.LogWarning("AI validation failed for {UseCase}: {Error}", useCase, error);
  }

  private sealed class ModuleChallengePlanResponse
  {
    public List<string>? SelectedWords { get; init; }
    public string? RecommendedGameType { get; init; }
    public int DifficultyLevel { get; init; }
    public string? Reason { get; init; }
    public string? FocusType { get; init; }
  }

}
