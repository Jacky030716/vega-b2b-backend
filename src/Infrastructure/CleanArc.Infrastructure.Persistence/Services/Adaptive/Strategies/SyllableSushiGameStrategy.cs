using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive.Strategies;

internal sealed class SyllableSushiGameStrategy : IGameStrategy
{
    public string GameKey => "syllable_sushi";
    public string GameTemplateCode => "SYLLABLE_SUSHI";
    public string Category => "STRUCTURE";

    public object GeneratePlayableContent(
        IReadOnlyList<AdaptiveChallengeItemDto> items,
        int difficultyLevel,
        string? configJson)
    {
        var syllableSushiSpecs = items.Select(ChallengeGameUtility.BuildSyllableSushiSpec).ToList();
        var primarySyllableSushiSpec = syllableSushiSpecs.FirstOrDefault();

        var mappedItems = items.Zip(syllableSushiSpecs, (item, spec) => new
        {
            vocabularyItemId = item.VocabularyItemId,
            word = item.Word,
            normalizedWord = item.NormalizedWord ?? item.Word.Trim().ToLowerInvariant(),
            hint = item.Hint,
            meaningText = item.MeaningText,
            exampleSentence = item.ExampleSentence,
            syllablesJson = ChallengeGameUtility.TryParseJson(item.SyllablesJson) ?? JsonNode.Parse("[]"),
            difficultyLevel = spec.DifficultyLevel,
            bmText = item.BmText,
            zhText = item.ZhText,
            enText = item.EnText,
            syllableText = item.SyllableText,
            itemType = item.ItemType,
            displayOrder = item.DisplayOrder,
            syllablePoolJson = JsonSerializer.SerializeToNode(spec.SyllablePool, ChallengeGameUtility.JsonOptions),
            distractorsJson = JsonSerializer.SerializeToNode(spec.Distractors, ChallengeGameUtility.JsonOptions),
            correctOrderJson = JsonSerializer.SerializeToNode(spec.CorrectOrder, ChallengeGameUtility.JsonOptions),
            spellCatcherSpecJson = JsonNode.Parse("null"),
            language = item.Language
        }).ToList();

        return new
        {
            gameTemplateCode = GameTemplateCode,
            category = Category,
            syllableSushiSpec = primarySyllableSushiSpec,
            syllableSushiSpecs = syllableSushiSpecs,
            items = mappedItems
        };
    }
}
