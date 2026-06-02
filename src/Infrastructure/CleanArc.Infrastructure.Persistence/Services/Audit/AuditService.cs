using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Audit;
using CleanArc.Application.Contracts.Persistence;

namespace CleanArc.Infrastructure.Persistence.Services.Audit;

public sealed class AuditService(
    IAdaptiveAnalyticsService adaptiveAnalyticsService,
    IClassroomRepository classroomRepository) : IAuditService
{
    private const int WeakWordsCap = 20;

    public async Task<ClassroomHealthDto> GetClassroomHealthAsync(int classroomId, CancellationToken cancellationToken)
    {
        var studentCount = await classroomRepository.GetStudentCountAsync(classroomId);
        var weakness = await adaptiveAnalyticsService.GetClassWeaknessOverviewAsync(classroomId, cancellationToken);
        var modules = await adaptiveAnalyticsService.GetModuleProgressSummaryAsync(classroomId, cancellationToken);

        var modulesWithVocabulary = modules.Where(m => m.VocabularyCount > 0).ToList();
        var averageMasteryScore = modulesWithVocabulary.Count == 0
            ? 0
            : Math.Round(modulesWithVocabulary.Average(m => m.AverageScore), 2);

        var modulesNeedingReviewCount = modules.Count(m =>
            string.Equals(m.Status, "REVIEW_NEEDED", StringComparison.OrdinalIgnoreCase)
            || (m.VocabularyCount > 0 && m.WeakWordCount > 0));

        var status = ResolveClassroomStatus(studentCount, weakness.WeakWordCount, modulesNeedingReviewCount);

        return new ClassroomHealthDto(
            classroomId,
            studentCount,
            weakness.WeakWordCount,
            weakness.OverdueReviewCount,
            averageMasteryScore,
            modulesNeedingReviewCount,
            status);
    }

    public async Task<StudentPerformanceAuditDto> GetStudentPerformanceAsync(int studentId, CancellationToken cancellationToken)
    {
        var performance = await adaptiveAnalyticsService.GetStudentPerformanceSummaryAsync(studentId, cancellationToken);
        var weakness = performance.WeaknessSummary;

        var weakWords = weakness.WeakWords
            .Select(w => w.Word)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(WeakWordsCap)
            .ToList();

        var averageMasteryScore = performance.Mastery.Count == 0
            ? 0
            : Math.Round((decimal)performance.Mastery.Average(m => m.MasteryScore), 2);

        var attemptCount = performance.Attempts.Sum(a => a.LegacyAttemptCount + a.AdaptiveAttemptCount);
        var completedChallengeCount = performance.Attempts.Sum(a =>
            a.CompletedLegacyAttemptCount + a.CompletedAdaptiveAttemptCount);

        return new StudentPerformanceAuditDto(
            studentId,
            null,
            weakness.WeakWordCount,
            weakness.OverdueReviewCount,
            averageMasteryScore,
            weakWords,
            attemptCount,
            completedChallengeCount);
    }

    public async Task<ModuleHealthDto> GetModuleHealthAsync(
        int classroomId,
        int moduleId,
        CancellationToken cancellationToken)
    {
        var modules = await adaptiveAnalyticsService.GetModuleProgressSummaryAsync(classroomId, cancellationToken);
        var module = modules.FirstOrDefault(m => m.ModuleId == moduleId);

        if (module is null)
        {
            var weakness = await adaptiveAnalyticsService.GetModuleWeaknessOverviewAsync(
                classroomId,
                moduleId,
                cancellationToken);

            return new ModuleHealthDto(
                classroomId,
                moduleId,
                0,
                weakness.WeakWordCount,
                0,
                0,
                0,
                weakness.WeakWordCount > 0 ? AuditHealthStatuses.NeedsReview : AuditHealthStatuses.NotStarted);
        }

        return new ModuleHealthDto(
            classroomId,
            moduleId,
            module.ProgressPercent,
            module.WeakWordCount,
            module.AverageScore,
            module.ChallengeCount,
            module.CompletedChallengeCount,
            module.Status);
    }

    public async Task<WeakWordsAuditDto> GetWeakWordsAsync(
        int classroomId,
        int? moduleId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<StudentWordMasteryDto> weakRows;
        if (moduleId is int scopedModuleId)
        {
            var overview = await adaptiveAnalyticsService.GetModuleWeaknessOverviewAsync(
                classroomId,
                scopedModuleId,
                cancellationToken);
            weakRows = overview.WeakWords;
        }
        else
        {
            var overview = await adaptiveAnalyticsService.GetClassWeaknessOverviewAsync(classroomId, cancellationToken);
            weakRows = overview.WeakWords;
        }

        var aggregatedWords = AggregateWeakWords(weakRows);
        var affectedStudents = weakRows
            .Select(row => row.StudentId)
            .Distinct()
            .Count();

        return new WeakWordsAuditDto(
            classroomId,
            moduleId,
            aggregatedWords,
            affectedStudents);
    }

    private static string ResolveClassroomStatus(int studentCount, int weakWordCount, int modulesNeedingReviewCount)
    {
        if (studentCount == 0)
            return AuditHealthStatuses.NotStarted;

        if (weakWordCount > 0 || modulesNeedingReviewCount > 0)
            return AuditHealthStatuses.NeedsReview;

        return AuditHealthStatuses.Healthy;
    }

    internal static IReadOnlyList<string> AggregateWeakWords(IReadOnlyList<StudentWordMasteryDto> weakRows)
    {
        return weakRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Word))
            .GroupBy(row => row.Word, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Word = group.Key,
                LowestMastery = group.Min(row => row.MasteryScore)
            })
            .OrderBy(item => item.LowestMastery)
            .ThenBy(item => item.Word, StringComparer.OrdinalIgnoreCase)
            .Take(WeakWordsCap)
            .Select(item => item.Word)
            .ToList();
    }
}
