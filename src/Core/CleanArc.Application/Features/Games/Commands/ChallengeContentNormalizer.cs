using System.Text.Json;
using System.Text.Json.Nodes;

namespace CleanArc.Application.Features.Games.Commands;

internal static class ChallengeContentNormalizer
{
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
        "spell_catcher" => NormalizeAdaptiveWordItems(rawContentData, "SPELL_CATCHER"),
        "syllable_sushi" => NormalizeAdaptiveWordItems(rawContentData, "SYLLABLE_SUSHI"),
        "voice_bridge" => NormalizeAdaptiveWordItems(rawContentData, "VOICE_BRIDGE"),
        "translation" => NormalizeAdaptiveWordItems(rawContentData, "TRANSLATION"),
        _ => NormalizationResult.Fail($"Unsupported game key '{gameKey}'")
      };
    }
    catch (JsonException)
    {
      return NormalizationResult.Fail("ContentData is not valid JSON");
    }
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
