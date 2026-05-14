using System.Text.Json;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;
public class AdaptiveAnalyticsService(
    ApplicationDbContext dbContext,
    IRecommendationEngine recommendationEngine) : IAdaptiveAnalyticsService
{
    public async Task<IReadOnlyList<StudentWordMasteryDto>> GetMasteryAsync(int studentId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.StudentWordMasteries.AsNoTracking()
            .Include(m => m.VocabularyItem)
            .Where(m => m.StudentId == studentId)
            .OrderBy(m => m.MasteryScore)
            .ThenBy(m => m.NextReviewAt)
            .ToListAsync(cancellationToken);

        var vocabularyIds = rows.Select(row => row.VocabularyItemId).ToArray();
        var errorPatterns = await dbContext.ErrorPatternLogs.AsNoTracking()
            .Where(log => log.StudentId == studentId && log.VocabularyItemId.HasValue && vocabularyIds.Contains(log.VocabularyItemId.Value))
            .GroupBy(log => log.VocabularyItemId!.Value)
            .Select(group => new
            {
                VocabularyItemId = group.Key,
                Patterns = group
                    .OrderByDescending(log => log.CreatedTime)
                    .Select(log => log.PatternType)
                    .Take(5)
                    .ToList()
            })
            .ToDictionaryAsync(item => item.VocabularyItemId, item => item.Patterns, cancellationToken);

        return rows.Select(m =>
        {
            errorPatterns.TryGetValue(m.VocabularyItemId, out var patterns);
            var dto = MasteryEngine.ToDto(m, m.VocabularyItem.Word);
            return dto with { ErrorPatternsJson = patterns is null ? null : JsonSerializer.Serialize(patterns.Distinct(), ChallengeGenerator.JsonOptions) };
        }).ToList();
    }

    public async Task<WeaknessSummaryDto> GetWeaknessSummaryAsync(int studentId, CancellationToken cancellationToken)
    {
        var mastery = await GetMasteryAsync(studentId, cancellationToken);
        var now = DateTime.UtcNow;
        var weak = mastery.Where(m => m.MasteryScore < 65).Take(20).ToList();
        var overdue = mastery.Count(m => m.NextReviewAt != null && m.NextReviewAt <= now);
        var recommended = await GetRecommendedNextChallengesAsync(studentId, cancellationToken);
        return new WeaknessSummaryDto(
            studentId,
            weak.Count,
            overdue,
            weak,
            recommended.Select(r => r.RecommendedGameTemplateCode).Distinct().ToList());
    }

    public Task<IReadOnlyList<AdaptiveRecommendationDto>> GetRecommendedNextChallengesAsync(int studentId, CancellationToken cancellationToken)
        => recommendationEngine.RecommendForStudentAsync(studentId, null, cancellationToken);

    public async Task<ClassWeaknessOverviewDto> GetClassWeaknessOverviewAsync(int classId, CancellationToken cancellationToken)
    {
        var studentIds = await dbContext.ClassroomStudents.AsNoTracking()
            .Where(cs => cs.ClassroomId == classId)
            .Select(cs => cs.UserId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var weakRows = await dbContext.StudentWordMasteries.AsNoTracking()
            .Include(m => m.VocabularyItem)
            .Where(m => studentIds.Contains(m.StudentId) && m.MasteryScore < 65)
            .OrderBy(m => m.MasteryScore)
            .Take(50)
            .ToListAsync(cancellationToken);
        var weak = weakRows.Select(m => MasteryEngine.ToDto(m, m.VocabularyItem.Word)).ToList();

        var overdue = await dbContext.StudentWordMasteries.AsNoTracking()
            .CountAsync(m => studentIds.Contains(m.StudentId) && m.NextReviewAt != null && m.NextReviewAt <= now, cancellationToken);

        return new ClassWeaknessOverviewDto(classId, weak.Count, overdue, weak);
    }

    public async Task<IReadOnlyList<ModuleProgressDto>> GetModuleProgressAsync(int classId, CancellationToken cancellationToken)
        => (await GetModuleProgressSummaryAsync(classId, cancellationToken))
            .Select(summary => new ModuleProgressDto(
                summary.ClassroomId,
                summary.ModuleId,
                summary.Title,
                summary.VocabularyCount,
                summary.ChallengeCount,
                summary.AverageScore))
            .ToList();

    public async Task<IReadOnlyList<ModuleProgressSummaryDto>> GetModuleProgressSummaryAsync(int classId, CancellationToken cancellationToken)
    {
        var studentIds = await dbContext.ClassroomStudents.AsNoTracking()
            .Where(cs => cs.ClassroomId == classId)
            .Select(cs => cs.UserId)
            .ToListAsync(cancellationToken);

        var modules = await dbContext.ClassroomModules.AsNoTracking()
            .Include(link => link.Module)
            .Where(link => link.ClassroomId == classId && link.Module.IsActive)
            .Select(link => link.Module)
            .OrderBy(module => module.Subject)
            .ThenBy(module => module.UnitNumber ?? int.MaxValue)
            .ThenBy(module => module.Title)
            .ToListAsync(cancellationToken);

        var moduleIds = modules.Select(module => module.Id).ToArray();
        var challengeStats = await dbContext.Challenges.AsNoTracking()
            .Where(challenge => challenge.ClassroomId == classId && challenge.ModuleId != null && moduleIds.Contains(challenge.ModuleId.Value))
            .GroupBy(challenge => challenge.ModuleId!.Value)
            .Select(group => new
            {
                ModuleId = group.Key,
                ChallengeCount = group.Count(),
                LastActivityAt = group.Max(challenge => challenge.LastActivityAt ?? challenge.ModifiedDate ?? challenge.CreatedTime)
            })
            .ToDictionaryAsync(item => item.ModuleId, item => item, cancellationToken);

        var challengeProgress = await dbContext.Challenges.AsNoTracking()
            .Where(challenge => challenge.ClassroomId == classId && challenge.ModuleId != null && moduleIds.Contains(challenge.ModuleId.Value))
            .Select(challenge => new
            {
                ModuleId = challenge.ModuleId!.Value,
                challenge.Id,
                Completed = challenge.LifecycleState == ChallengeLifecycleState.Completed ||
                    dbContext.ChallengeProgresses.AsNoTracking().Any(progress =>
                        progress.ClassroomId == classId &&
                        progress.ChallengeId == challenge.Id &&
                        progress.HasCompleted)
            })
            .GroupBy(item => item.ModuleId)
            .Select(group => new
            {
                ModuleId = group.Key,
                Total = group.Count(),
                Completed = group.Count(item => item.Completed)
            })
            .ToDictionaryAsync(item => item.ModuleId, item => item, cancellationToken);

        var now = DateTime.UtcNow;
        var result = new List<ModuleProgressSummaryDto>();
        foreach (var module in modules)
        {
            var vocabularyIds = await dbContext.VocabularyItems.AsNoTracking()
                .Where(v => v.ModuleId == module.Id && v.IsActive)
                .Select(v => v.Id)
                .ToListAsync(cancellationToken);

            var rows = await dbContext.StudentWordMasteries.AsNoTracking()
                .Where(m => studentIds.Contains(m.StudentId) && vocabularyIds.Contains(m.VocabularyItemId))
                .ToListAsync(cancellationToken);

            challengeProgress.TryGetValue(module.Id, out var progress);
            challengeStats.TryGetValue(module.Id, out var challengeStat);
            var progressPercent = progress is null || progress.Total == 0 ? 0 : (int)Math.Round((double)progress.Completed / progress.Total * 100);
            var weakWordCount = rows.Count(row => row.MasteryScore < 65);
            result.Add(new ModuleProgressSummaryDto(
                classId,
                module.Id,
                string.IsNullOrWhiteSpace(module.UnitTitle) ? module.Title : module.UnitTitle,
                module.Subject,
                module.YearLevel,
                vocabularyIds.Count,
                challengeStat?.ChallengeCount ?? 0,
                progress?.Completed ?? 0,
                progressPercent,
                weakWordCount,
                rows.Count == 0 ? 0 : Math.Round((decimal)rows.Average(r => r.MasteryScore), 2),
                challengeStat?.LastActivityAt,
                ResolveModuleProgressStatus(challengeStat?.ChallengeCount ?? 0, progressPercent, weakWordCount)));
        }

        return result;
    }

    public async Task<StudentPerformanceDto> GetStudentPerformanceAsync(int studentId, CancellationToken cancellationToken)
    {
        var mastery = await GetMasteryAsync(studentId, cancellationToken);
        var weakness = await GetWeaknessSummaryAsync(studentId, cancellationToken);
        var recommendations = await GetRecommendedNextChallengesAsync(studentId, cancellationToken);
        return new StudentPerformanceDto(studentId, mastery, weakness, recommendations);
    }

    public async Task<StudentPerformanceSummaryDto> GetStudentPerformanceSummaryAsync(int studentId, CancellationToken cancellationToken)
    {
        var mastery = await GetMasteryAsync(studentId, cancellationToken);
        var weakness = await GetWeaknessSummaryAsync(studentId, cancellationToken);
        var recommendations = await GetRecommendedNextChallengesAsync(studentId, cancellationToken);
        var attempts = await GetAttemptSummariesAsync(studentId, cancellationToken);

        return new StudentPerformanceSummaryDto(
            studentId,
            mastery.Select(ToSummary).ToList(),
            weakness,
            attempts,
            recommendations);
    }

    private async Task<IReadOnlyList<ChallengeAttemptSummaryDto>> GetAttemptSummariesAsync(
        int studentId,
        CancellationToken cancellationToken)
    {
        var legacy = await dbContext.Attempts.AsNoTracking()
            .Where(attempt => attempt.UserId == studentId)
            .GroupBy(attempt => attempt.ChallengeId)
            .Select(group => new
            {
                ChallengeId = group.Key,
                LegacyAttemptCount = group.Count(),
                CompletedLegacyAttemptCount = group.Count(attempt => attempt.IsCompleted),
                BestScore = group.Max(attempt => attempt.Score),
                BestStars = group.Max(attempt => attempt.StarsEarned),
                LastAttemptAt = group.Max(attempt => (DateTime?)attempt.CompletedAt)
            })
            .ToListAsync(cancellationToken);

        var adaptive = await dbContext.StudentChallengeAttempts.AsNoTracking()
            .Where(attempt => attempt.StudentId == studentId)
            .Select(attempt => new
            {
                attempt.ChallengeId,
                attempt.CompletionStatus,
                ItemCount = dbContext.StudentChallengeItemAttempts.AsNoTracking()
                    .Count(item => item.StudentChallengeAttemptId == attempt.Id)
            })
            .GroupBy(attempt => attempt.ChallengeId)
            .Select(group => new
            {
                ChallengeId = group.Key,
                AdaptiveAttemptCount = group.Count(),
                CompletedAdaptiveAttemptCount = group.Count(attempt => attempt.CompletionStatus == "completed"),
                ItemAttemptCount = group.Sum(attempt => attempt.ItemCount)
            })
            .ToListAsync(cancellationToken);

        var adaptiveByChallenge = adaptive.ToDictionary(item => item.ChallengeId);
        return legacy.Select(item =>
        {
            adaptiveByChallenge.TryGetValue(item.ChallengeId, out var adaptiveItem);
            return new ChallengeAttemptSummaryDto(
                item.ChallengeId,
                studentId,
                item.LegacyAttemptCount,
                adaptiveItem?.AdaptiveAttemptCount ?? 0,
                item.CompletedLegacyAttemptCount,
                adaptiveItem?.CompletedAdaptiveAttemptCount ?? 0,
                adaptiveItem?.ItemAttemptCount ?? 0,
                item.BestScore,
                item.BestStars,
                item.LastAttemptAt);
        }).ToList();
    }

    private static WordMasterySummaryDto ToSummary(StudentWordMasteryDto mastery) => new(
        mastery.Id,
        mastery.StudentId,
        mastery.VocabularyItemId,
        mastery.ModuleId,
        mastery.Word,
        mastery.MasteryScore,
        mastery.MasteryLevel,
        mastery.TotalAttempts,
        mastery.CorrectAttempts,
        mastery.LastPracticedAt,
        mastery.NextReviewAt,
        mastery.IsDueForReview,
        mastery.WeaknessTagsJson,
        mastery.ErrorPatternsJson);

    private static string ResolveModuleProgressStatus(int challengeCount, int progressPercent, int weakWordCount)
    {
        if (challengeCount == 0)
            return "NOT_STARTED";
        if (progressPercent >= 100)
            return weakWordCount > 0 ? "REVIEW_NEEDED" : "COMPLETED";
        if (progressPercent > 0)
            return "IN_PROGRESS";
        return "ASSIGNED";
    }
}

