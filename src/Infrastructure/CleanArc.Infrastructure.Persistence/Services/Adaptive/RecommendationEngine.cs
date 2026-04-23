using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;
public class RecommendationEngine(ApplicationDbContext dbContext) : IRecommendationEngine
{
    public async Task<IReadOnlyList<AdaptiveRecommendationDto>> RecommendForStudentAsync(
        int studentId,
        GenerateAdaptiveChallengeRequest? context,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var weak = await dbContext.StudentWordMasteries.AsNoTracking()
            .Include(m => m.VocabularyItem)
            .Where(m => m.StudentId == studentId && (m.MasteryScore < 65 || (m.NextReviewAt != null && m.NextReviewAt <= now)))
            .OrderBy(m => m.NextReviewAt != null && m.NextReviewAt <= now ? 0 : 1)
            .ThenBy(m => m.MasteryScore)
            .Take(12)
            .ToListAsync(cancellationToken);

        var items = weak.Select(m => new AdaptiveChallengeItemDto(
            null,
            m.VocabularyItemId,
            m.VocabularyItem.Word,
            m.VocabularyItem.NormalizedWord,
            m.VocabularyItem.PhoneticHint ?? m.VocabularyItem.MeaningText,
            m.VocabularyItem.MeaningText,
            m.VocabularyItem.ExampleSentence,
            m.VocabularyItem.SyllablesJson,
            m.VocabularyItem.DifficultyLevel,
            m.VocabularyItem.BmText,
            m.VocabularyItem.ZhText,
            m.VocabularyItem.EnText,
            m.VocabularyItem.SyllableText,
            m.VocabularyItem.ItemType,
            m.VocabularyItem.DisplayOrder,
            null,
            null,
            null,
            null)).ToList();

        if (items.Count == 0 && context?.ModuleId is int moduleId)
        {
            items = await dbContext.VocabularyItems.AsNoTracking()
                .Where(v => v.ModuleId == moduleId && v.IsActive)
                .OrderBy(v => v.DisplayOrder)
                .ThenBy(v => v.Word)
                .Take(12)
                .Select(v => new AdaptiveChallengeItemDto(null, v.Id, v.Word, v.NormalizedWord, v.PhoneticHint ?? v.MeaningText, v.MeaningText, v.ExampleSentence, v.SyllablesJson, v.DifficultyLevel, v.BmText, v.ZhText, v.EnText, v.SyllableText, v.ItemType, v.DisplayOrder, null, null, null, null))
                .ToListAsync(cancellationToken);
        }

        var tags = string.Join(' ', weak.Select(w => w.WeaknessTagsJson)).ToLowerInvariant();
        var overdue = weak.Any(w => w.NextReviewAt != null && w.NextReviewAt <= now);
        var code = tags.Contains("syllable")
            ? "SYLLABLE_SUSHI"
            : tags.Contains("pronunciation") || tags.Contains("oral")
                ? "VOICE_BRIDGE"
                : "SPELL_CATCHER";

        var objective = overdue ? "review_overdue_words" : context?.Objective ?? "improve_weak_words";
        var reason = overdue
            ? "Some words are due for review."
            : code switch
            {
                "SYLLABLE_SUSHI" => "Recent attempts show weak syllable assembly.",
                "VOICE_BRIDGE" => "Recent attempts show weak oral recall or pronunciation.",
                _ => "Recent attempts show weak full spelling recall."
            };

        return new[]
        {
            new AdaptiveRecommendationDto(objective, code, ChallengeGenerator.ToGameKey(code), reason, items.Count, items)
        };
    }
}

