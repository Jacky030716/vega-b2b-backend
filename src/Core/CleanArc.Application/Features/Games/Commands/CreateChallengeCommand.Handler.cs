using System.Text.Json;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.Quiz.Content;
using Mediator;

namespace CleanArc.Application.Features.Games.Commands;

internal sealed class CreateChallengeCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateChallengeCommand, OperationResult<CreateChallengeDto>>
{
  private const string WordBridgeImageRefPrefix = "quizzes/word-bridge/";

  private static readonly JsonSerializerOptions _camelCase = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  public async ValueTask<OperationResult<CreateChallengeDto>> Handle(
      CreateChallengeCommand request,
      CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.GameKey))
      return OperationResult<CreateChallengeDto>.FailureResult("Game key is required");

    if (string.IsNullOrWhiteSpace(request.Title))
      return OperationResult<CreateChallengeDto>.FailureResult("Title is required");

    if (request.DifficultyLevel < 1 || request.DifficultyLevel > 5)
      return OperationResult<CreateChallengeDto>.FailureResult("DifficultyLevel must be between 1 and 5");

    var game = await unitOfWork.ChallengeRepository.GetGameByKeyAsync(request.GameKey.Trim());
    if (game is null)
      return OperationResult<CreateChallengeDto>.NotFoundResult($"Game '{request.GameKey}' not found");

    var normalizedContentResult = NormalizeAndValidateContentData(game.Key, request.ContentData);
    if (!normalizedContentResult.IsSuccess)
      return OperationResult<CreateChallengeDto>.FailureResult(normalizedContentResult.ErrorMessage!);

    var nextOrderIndex = await unitOfWork.ChallengeRepository.GetNextOrderIndexForGameAsync(game.Id);

    var challenge = await unitOfWork.ChallengeRepository.CreateChallengeAsync(new Challenge
    {
      GameId = game.Id,
      Title = request.Title.Trim(),
      Description = request.Description?.Trim() ?? string.Empty,
      DifficultyLevel = request.DifficultyLevel,
      ContentData = normalizedContentResult.Result!,
      OrderIndex = nextOrderIndex,
      MaxStars = 3,
      CreatedById = request.UserId,
      IsAIGenerated = request.IsAIGenerated
    });

    return OperationResult<CreateChallengeDto>.SuccessResult(new CreateChallengeDto(
      challenge.Id,
      game.Id,
      game.Key,
      challenge.Title,
      challenge.DifficultyLevel,
      challenge.OrderIndex,
      challenge.IsAIGenerated,
      challenge.CreatedById
    ));
  }

  private static NormalizationResult NormalizeAndValidateContentData(string gameKey, string rawContentData)
  {
    if (string.IsNullOrWhiteSpace(rawContentData))
      return NormalizationResult.Fail("ContentData is required");

    try
    {
      return gameKey switch
      {
        "word_bridge" => NormalizeWordBridgeContent(rawContentData),
        "magic_backpack" => NormalizeMagicBackpackContent(rawContentData),
        "word_pair" => NormalizeWordPairContent(rawContentData),
        _ => NormalizationResult.Fail($"Unsupported game key '{gameKey}'")
      };
    }
    catch (JsonException)
    {
      return NormalizationResult.Fail("ContentData is not valid JSON");
    }
  }

  private static NormalizationResult NormalizeWordBridgeContent(string rawContentData)
  {
    var parsed = JsonSerializer.Deserialize<WordBridgeContent>(rawContentData);
    if (parsed is null || parsed.Words.Count == 0)
      return NormalizationResult.Fail("word_bridge content must include at least one word");

    foreach (var word in parsed.Words)
    {
      if (string.IsNullOrWhiteSpace(word.Target))
        return NormalizationResult.Fail("word_bridge word.target is required");

      word.Target = word.Target.Trim().ToUpperInvariant();
      word.Translation = word.Translation?.Trim() ?? string.Empty;
      word.Difficulty = string.IsNullOrWhiteSpace(word.Difficulty)
          ? "easy"
          : word.Difficulty.Trim().ToLowerInvariant();

      if (string.IsNullOrWhiteSpace(word.ImageRef))
      {
        word.ImageRef = null;
        continue;
      }

      var normalizedImageRef = word.ImageRef.Trim().TrimStart('/');
      if (!normalizedImageRef.StartsWith(WordBridgeImageRefPrefix, StringComparison.OrdinalIgnoreCase))
      {
        return NormalizationResult.Fail($"word_bridge word.imageRef must start with '{WordBridgeImageRefPrefix}'");
      }

      word.ImageRef = normalizedImageRef;
    }

    return NormalizationResult.Ok(JsonSerializer.Serialize(parsed, _camelCase));
  }

  private static NormalizationResult NormalizeMagicBackpackContent(string rawContentData)
  {
    var parsed = JsonSerializer.Deserialize<MagicBackpackContent>(rawContentData);
    if (parsed is null)
      return NormalizationResult.Fail("magic_backpack content is required");

    var items = parsed.Items
      .Where(item => !string.IsNullOrWhiteSpace(item))
      .Select(item => item.Trim())
      .ToList();

    if (items.Count == 0)
      return NormalizationResult.Fail("magic_backpack content must include at least one item");

    var normalized = new MagicBackpackContent
    {
      Theme = string.IsNullOrWhiteSpace(parsed.Theme) ? "custom" : parsed.Theme.Trim(),
      SequenceLength = parsed.SequenceLength is > 0 ? parsed.SequenceLength : items.Count,
      GhostMode = parsed.GhostMode,
      Items = items
    };

    return NormalizationResult.Ok(JsonSerializer.Serialize(normalized, _camelCase));
  }

  private static NormalizationResult NormalizeWordPairContent(string rawContentData)
  {
    var parsed = JsonSerializer.Deserialize<WordTwinsContent>(rawContentData);
    if (parsed is null || parsed.Pairs.Count == 0)
      return NormalizationResult.Fail("word_pair content must include at least one pair");

    foreach (var pair in parsed.Pairs)
    {
      if (string.IsNullOrWhiteSpace(pair.Word))
        return NormalizationResult.Fail("word_pair pair.word is required");

      pair.Word = pair.Word.Trim();
      pair.Translation = pair.Translation?.Trim();
      pair.ImageKey = pair.ImageKey?.Trim();
      pair.ImageRef = pair.ImageRef?.Trim();
    }

    return NormalizationResult.Ok(JsonSerializer.Serialize(parsed, _camelCase));
  }

  private sealed class MagicBackpackContent
  {
    public string? Theme { get; init; }
    public int? SequenceLength { get; init; }
    public bool? GhostMode { get; init; }
    public List<string> Items { get; init; } = new();
  }

  private sealed class NormalizationResult
  {
    public bool IsSuccess { get; init; }
    public string? Result { get; init; }
    public string? ErrorMessage { get; init; }

    public static NormalizationResult Ok(string result) => new()
    {
      IsSuccess = true,
      Result = result
    };

    public static NormalizationResult Fail(string errorMessage) => new()
    {
      IsSuccess = false,
      ErrorMessage = errorMessage
    };
  }
}
