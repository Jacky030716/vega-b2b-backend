using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive.Strategies;

internal sealed class VoiceBridgeGameStrategy : IGameStrategy
{
    public string GameKey => "voice_bridge";
    public string GameTemplateCode => "VOICE_BRIDGE";
    public string Category => "SPEAKING";

    public object GeneratePlayableContent(
        IReadOnlyList<AdaptiveChallengeItemDto> items,
        int difficultyLevel,
        string? configJson)
    {
        var mappedItems = items.Select(item => new
        {
            vocabularyItemId = item.VocabularyItemId,
            word = item.Word,
            normalizedWord = item.NormalizedWord ?? item.Word.Trim().ToLowerInvariant(),
            hint = item.Hint,
            meaningText = item.MeaningText,
            exampleSentence = item.ExampleSentence,
            syllablesJson = ChallengeGameUtility.TryParseJson(item.SyllablesJson) ?? JsonNode.Parse("[]"),
            difficultyLevel = item.DifficultyLevel,
            bmText = item.BmText,
            zhText = item.ZhText,
            enText = item.EnText,
            syllableText = item.SyllableText,
            itemType = item.ItemType,
            displayOrder = item.DisplayOrder,
            syllablePoolJson = JsonNode.Parse("[]"),
            distractorsJson = JsonNode.Parse("[]"),
            correctOrderJson = JsonNode.Parse("[]"),
            spellCatcherSpecJson = JsonNode.Parse("null"),
            language = item.Language
        }).ToList();

        return new
        {
            gameTemplateCode = GameTemplateCode,
            category = Category,
            items = mappedItems
        };
    }
}
