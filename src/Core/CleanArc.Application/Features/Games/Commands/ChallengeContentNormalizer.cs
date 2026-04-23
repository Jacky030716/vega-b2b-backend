using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Domain.Entities.Quiz.Content;

namespace CleanArc.Application.Features.Games.Commands;

internal static class ChallengeContentNormalizer
{
  private const string WordBridgeImageRefPrefix = "quizzes/word-bridge/";

  private static readonly JsonSerializerOptions CamelCase = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  public static NormalizationResult NormalizeAndValidate(string gameKey, string rawContentData)
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
        "spell_catcher" => NormalizeAdaptiveWordItems(rawContentData, "SPELL_CATCHER"),
        "syllable_sushi" => NormalizeAdaptiveWordItems(rawContentData, "SYLLABLE_SUSHI"),
        "voice_bridge" => NormalizeAdaptiveWordItems(rawContentData, "VOICE_BRIDGE"),
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
        return NormalizationResult.Fail($"word_bridge word.imageRef must start with '{WordBridgeImageRefPrefix}'");

      word.ImageRef = normalizedImageRef;
    }

    return NormalizationResult.Ok(JsonSerializer.Serialize(parsed, CamelCase));
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

    return NormalizationResult.Ok(JsonSerializer.Serialize(normalized, CamelCase));
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

    return NormalizationResult.Ok(JsonSerializer.Serialize(parsed, CamelCase));
  }

  private static NormalizationResult NormalizeAdaptiveWordItems(string rawContentData, string expectedTemplateCode)
  {
    var root = JsonNode.Parse(rawContentData)?.AsObject();
    if (root is null)
      return NormalizationResult.Fail($"{expectedTemplateCode} content must be a JSON object");

    var items = root["items"]?.AsArray();
    if (items is null || items.Count == 0)
      return NormalizationResult.Fail($"{expectedTemplateCode} content must include at least one item");

    root["gameTemplateCode"] = expectedTemplateCode;

    foreach (var itemNode in items)
    {
      if (itemNode is not JsonObject item)
        return NormalizationResult.Fail($"{expectedTemplateCode} items must be objects");

      var word = item["word"]?.GetValue<string>()?.Trim();
      if (string.IsNullOrWhiteSpace(word))
        return NormalizationResult.Fail($"{expectedTemplateCode} item.word is required");

      item["word"] = word;
      item["normalizedWord"] = item["normalizedWord"]?.GetValue<string>()?.Trim().ToLowerInvariant()
          ?? word.ToLowerInvariant();
    }

    return NormalizationResult.Ok(root.ToJsonString());
  }

  private sealed class MagicBackpackContent
  {
    public string? Theme { get; init; }
    public int? SequenceLength { get; init; }
    public bool? GhostMode { get; init; }
    public List<string> Items { get; init; } = new();
  }

  public sealed class NormalizationResult
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
