using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive.Strategies;

internal sealed class SpellCatcherGameStrategy : IGameStrategy
{
    public string GameKey => "spell_catcher";
    public string GameTemplateCode => "SPELL_CATCHER";
    public string Category => "RECALL";

    public object GeneratePlayableContent(
        IReadOnlyList<AdaptiveChallengeItemDto> items,
        int difficultyLevel,
        string? configJson)
    {
        // Solve difficulty level if items have different levels
        var resolvedDifficulty = difficultyLevel;
        if (items.Count > 0)
        {
            resolvedDifficulty = Math.Clamp((int)Math.Round(items.Average(i => i.DifficultyLevel)), 1, 3);
        }

        Dictionary<int, SpellCatcherWeakness>? weaknesses = null;
        if (!string.IsNullOrWhiteSpace(configJson))
        {
            try
            {
                var doc = JsonDocument.Parse(configJson);
                if (doc.RootElement.TryGetProperty("weaknesses", out var prop) && prop.ValueKind != JsonValueKind.Null)
                {
                    weaknesses = JsonSerializer.Deserialize<Dictionary<int, SpellCatcherWeakness>>(prop.GetRawText(), ChallengeGameUtility.JsonOptions);
                }
            }
            catch {}
        }

        var spellCatcherSpecs = items
            .Select(item => {
                var weakness = (item.VocabularyItemId is int vocabId && weaknesses != null && weaknesses.TryGetValue(vocabId, out var w))
                    ? w
                    : new SpellCatcherWeakness(true, false, true);

                return ChallengeGameUtility.BuildSpellCatcherSpec(
                    item,
                    needsMeaningSupport: resolvedDifficulty < 3 || weakness.NeedsMeaningSupport,
                    needsSyllableSupport: weakness.NeedsSyllableSupport,
                    needsAudioSupport: resolvedDifficulty == 1 || weakness.NeedsAudioSupport);
            })
            .ToList();

        var primarySpellCatcherSpec = spellCatcherSpecs.FirstOrDefault();

        var mappedItems = items.Zip(spellCatcherSpecs, (item, spec) => new
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
            syllablePoolJson = JsonNode.Parse("[]"),
            distractorsJson = JsonNode.Parse("[]"),
            correctOrderJson = JsonNode.Parse("[]"),
            spellCatcherSpecJson = JsonSerializer.SerializeToNode(spec, ChallengeGameUtility.JsonOptions),
            language = item.Language
        }).ToList();

        return new
        {
            gameTemplateCode = GameTemplateCode,
            category = Category,
            spellCatcherSpec = primarySpellCatcherSpec,
            spellCatcherSpecs = spellCatcherSpecs,
            items = mappedItems
        };
    }
}
