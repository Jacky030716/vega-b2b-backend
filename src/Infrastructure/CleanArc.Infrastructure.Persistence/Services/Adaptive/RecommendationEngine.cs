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
        var progresses = await dbContext.WordProgresses.AsNoTracking()
            .Include(wp => wp.Word)
            .Where(wp => wp.StudentId == studentId && wp.Word.IsActive)
            .ToListAsync(cancellationToken);

        var weak = progresses.Select(wp =>
        {
            int decayedScore = MasteryEngine.GetDecayedMasteryScore(wp.MasteryScore, wp.LastPracticedAt);
            bool isOverdue = wp.NextReviewDate.HasValue && now >= wp.NextReviewDate.Value;
            int priority = 5; // default Mastered and on-time

            if (isOverdue)
            {
                priority = 1; // Overdue review
            }
            else if (decayedScore < 50)
            {
                priority = 2; // Weak
            }
            else if (decayedScore < 80)
            {
                priority = 3; // Developing
            }

            return new { Progress = wp, Priority = priority, DecayedScore = decayedScore, IsOverdue = isOverdue };
        })
        .Where(x => x.Priority <= 3)
        .OrderBy(x => x.Priority)
        .ThenBy(x => x.DecayedScore)
        .Take(12)
        .ToList();

        var selectedProgresses = weak.Select(x => x.Progress).ToList();
        var vocabularyIds = selectedProgresses.Select(wp => wp.WordId).ToList();

        var masteries = await dbContext.StudentWordMasteries.AsNoTracking()
            .Where(m => m.StudentId == studentId && vocabularyIds.Contains(m.VocabularyItemId))
            .ToDictionaryAsync(m => m.VocabularyItemId, cancellationToken);

        var items = weak.Select(x => {
            var wp = x.Progress;
            return new AdaptiveChallengeItemDto(
                null,
                wp.WordId,
                wp.Word.Word,
                wp.Word.NormalizedWord,
                wp.Word.PhoneticHint ?? wp.Word.MeaningText,
                wp.Word.MeaningText,
                wp.Word.ExampleSentence,
                wp.Word.SyllablesJson,
                wp.Word.DifficultyLevel,
                wp.Word.BmText,
                wp.Word.ZhText,
                wp.Word.EnText,
                wp.Word.SyllableText,
                wp.Word.ItemType,
                wp.Word.DisplayOrder,
                null,
                null,
                null,
                null,
                wp.Word.Language);
        }).ToList();

        if (items.Count == 0 && context?.ModuleId is int moduleId)
        {
            items = await dbContext.VocabularyItems.AsNoTracking()
                .Where(v => v.ModuleId == moduleId && v.IsActive)
                .OrderBy(v => v.DisplayOrder)
                .ThenBy(v => v.Word)
                .Take(12)
                .Select(v => new AdaptiveChallengeItemDto(null, v.Id, v.Word, v.NormalizedWord, v.PhoneticHint ?? v.MeaningText, v.MeaningText, v.ExampleSentence, v.SyllablesJson, v.DifficultyLevel, v.BmText, v.ZhText, v.EnText, v.SyllableText, v.ItemType, v.DisplayOrder, null, null, null, null, v.Language))
                .ToListAsync(cancellationToken);
        }

        var tagList = new List<string>();
        foreach (var x in weak)
        {
            if (masteries.TryGetValue(x.Progress.WordId, out var m) && !string.IsNullOrWhiteSpace(m.WeaknessTagsJson))
            {
                tagList.Add(m.WeaknessTagsJson);
            }
        }
        var tags = string.Join(' ', tagList).ToLowerInvariant();
        var overdue = weak.Any(x => x.IsOverdue);
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

