using CleanArc.Application.Contracts.Adaptive;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

public sealed class AttemptConsistencyService(ApplicationDbContext dbContext) : IAttemptConsistencyService
{
    public async Task<AttemptConsistencyHealthDto> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var completedLegacyAttempts = await dbContext.Attempts.AsNoTracking()
            .Where(attempt => attempt.IsCompleted)
            .Select(attempt => new
            {
                attempt.UserId,
                attempt.ChallengeId
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var adaptiveAttempts = await dbContext.StudentChallengeAttempts.AsNoTracking()
            .Select(attempt => new
            {
                attempt.StudentId,
                attempt.ChallengeId
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var adaptiveKeys = adaptiveAttempts
            .Select(attempt => (attempt.StudentId, attempt.ChallengeId))
            .ToHashSet();
        var legacyKeys = completedLegacyAttempts
            .Select(attempt => (attempt.UserId, attempt.ChallengeId))
            .ToHashSet();

        var missingAdaptive = completedLegacyAttempts
            .Where(attempt => !adaptiveKeys.Contains((attempt.UserId, attempt.ChallengeId)))
            .ToList();
        var missingLegacy = adaptiveAttempts
            .Where(attempt => !legacyKeys.Contains((attempt.StudentId, attempt.ChallengeId)))
            .ToList();

        var adaptiveAttemptTelemetry = await dbContext.StudentChallengeAttempts.AsNoTracking()
            .Select(attempt => new
            {
                attempt.Id,
                attempt.StudentId,
                attempt.ChallengeId,
                attempt.CompletionStatus,
                ItemCount = dbContext.StudentChallengeItemAttempts.AsNoTracking()
                    .Count(item => item.StudentChallengeAttemptId == attempt.Id)
            })
            .ToListAsync(cancellationToken);
        var missingItemTelemetry = adaptiveAttemptTelemetry
            .Where(attempt =>
                string.Equals(attempt.CompletionStatus, "completed", StringComparison.OrdinalIgnoreCase) &&
                attempt.ItemCount == 0)
            .ToList();

        var itemTelemetry = await dbContext.StudentChallengeItemAttempts.AsNoTracking()
            .Include(item => item.StudentChallengeAttempt)
            .Where(item => item.VocabularyItemId.HasValue)
            .Select(item => new
            {
                item.StudentChallengeAttempt.StudentId,
                item.StudentChallengeAttempt.ChallengeId,
                VocabularyItemId = item.VocabularyItemId!.Value
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var masteryRows = await dbContext.StudentWordMasteries.AsNoTracking()
            .Select(mastery => new
            {
                mastery.StudentId,
                mastery.VocabularyItemId
            })
            .ToListAsync(cancellationToken);
        var masteryKeys = masteryRows
            .Select(mastery => (mastery.StudentId, mastery.VocabularyItemId))
            .ToHashSet();

        var missingMastery = itemTelemetry
            .Where(item => !masteryKeys.Contains((item.StudentId, item.VocabularyItemId)))
            .ToList();

        var affectedStudentIds = missingAdaptive.Select(item => item.UserId)
            .Concat(missingLegacy.Select(item => item.StudentId))
            .Concat(missingItemTelemetry.Select(item => item.StudentId))
            .Concat(missingMastery.Select(item => item.StudentId))
            .Distinct()
            .OrderBy(id => id)
            .ToList();
        var affectedChallengeIds = missingAdaptive.Select(item => item.ChallengeId)
            .Concat(missingLegacy.Select(item => item.ChallengeId))
            .Concat(missingItemTelemetry.Select(item => item.ChallengeId))
            .Concat(missingMastery.Select(item => item.ChallengeId))
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var severity = missingMastery.Count > 0 || missingLegacy.Count > 0 || missingItemTelemetry.Count > 0
            ? "critical"
            : missingAdaptive.Count > 0
                ? "warning"
                : "healthy";
        var suggestedFix = severity == "healthy"
            ? "No repair needed."
            : "Review affected attempts. Safe repair may rebuild aggregate ChallengeProgress only; do not synthesize item telemetry or mastery without source item data.";

        return new AttemptConsistencyHealthDto(
            missingAdaptive.Count,
            missingLegacy.Count,
            missingItemTelemetry.Count,
            missingMastery.Count,
            affectedStudentIds,
            affectedChallengeIds,
            severity,
            suggestedFix,
            DateTime.UtcNow);
    }

    public async Task<AttemptConsistencyReportDto> CheckClassroomAsync(
        int classroomId,
        int teacherId,
        bool isAdmin,
        int? moduleId,
        int? studentId,
        int? challengeId,
        CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == classroomId && item.IsActive && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found");

        if (!isAdmin && classroom.TeacherId != teacherId)
            throw new UnauthorizedAccessException("You do not manage this classroom");

        var classroomStudentIds = await dbContext.ClassroomStudents.AsNoTracking()
            .Where(item => item.ClassroomId == classroomId)
            .Select(item => item.UserId)
            .ToListAsync(cancellationToken);

        if (studentId.HasValue && !classroomStudentIds.Contains(studentId.Value))
            throw new InvalidOperationException("Student is not in this classroom");

        var challengesQuery = dbContext.Challenges.AsNoTracking()
            .Where(challenge => challenge.ClassroomId == classroomId);

        if (moduleId.HasValue)
            challengesQuery = challengesQuery.Where(challenge => challenge.ModuleId == moduleId.Value);
        if (challengeId.HasValue)
            challengesQuery = challengesQuery.Where(challenge => challenge.Id == challengeId.Value);

        var challenges = await challengesQuery
            .Select(challenge => new
            {
                challenge.Id,
                challenge.ModuleId
            })
            .ToListAsync(cancellationToken);

        var challengeIds = challenges.Select(challenge => challenge.Id).ToArray();
        var moduleByChallenge = challenges.ToDictionary(challenge => challenge.Id, challenge => challenge.ModuleId);
        var studentIds = studentId.HasValue ? new[] { studentId.Value } : classroomStudentIds.ToArray();
        var issues = new List<AttemptConsistencyIssueDto>();

        var legacyGroups = await dbContext.Attempts.AsNoTracking()
            .Where(attempt => challengeIds.Contains(attempt.ChallengeId) && studentIds.Contains(attempt.UserId))
            .GroupBy(attempt => new { attempt.UserId, attempt.ChallengeId })
            .Select(group => new
            {
                group.Key.UserId,
                group.Key.ChallengeId,
                AttemptCount = group.Count(),
                CompletedCount = group.Count(attempt => attempt.IsCompleted),
                BestScore = group.Where(attempt => attempt.IsCompleted).Select(attempt => (int?)attempt.Score).Max() ?? 0,
                BestStars = group.Where(attempt => attempt.IsCompleted).Select(attempt => (int?)attempt.StarsEarned).Max() ?? 0,
                FirstLegacyAttemptId = group.Select(attempt => (int?)attempt.Id).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        foreach (var legacy in legacyGroups)
        {
            var progress = await dbContext.ChallengeProgresses.AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.ClassroomId == classroomId &&
                    item.UserId == legacy.UserId &&
                    item.ChallengeId == legacy.ChallengeId,
                    cancellationToken);

            moduleByChallenge.TryGetValue(legacy.ChallengeId, out var issueModuleId);
            if (progress is null)
            {
                issues.Add(new AttemptConsistencyIssueDto(
                    "critical",
                    "missing_challenge_progress",
                    "A legacy attempt exists but ChallengeProgress is missing.",
                    legacy.UserId,
                    legacy.ChallengeId,
                    issueModuleId,
                    legacy.FirstLegacyAttemptId,
                    null));
                continue;
            }

            if (progress.AttemptCount != legacy.AttemptCount)
            {
                issues.Add(new AttemptConsistencyIssueDto(
                    "warning",
                    "stale_progress_attempt_count",
                    $"ChallengeProgress attempt count is {progress.AttemptCount}, expected {legacy.AttemptCount}.",
                    legacy.UserId,
                    legacy.ChallengeId,
                    issueModuleId,
                    legacy.FirstLegacyAttemptId,
                    null));
            }

            if (legacy.CompletedCount > 0 && !progress.HasCompleted)
            {
                issues.Add(new AttemptConsistencyIssueDto(
                    "critical",
                    "stale_progress_completion",
                    "A completed legacy attempt exists but ChallengeProgress is not marked completed.",
                    legacy.UserId,
                    legacy.ChallengeId,
                    issueModuleId,
                    legacy.FirstLegacyAttemptId,
                    null));
            }

            if (progress.BestScore != legacy.BestScore || progress.BestStars != legacy.BestStars)
            {
                issues.Add(new AttemptConsistencyIssueDto(
                    "warning",
                    "stale_progress_best_result",
                    $"ChallengeProgress best result is {progress.BestScore}/{progress.BestStars}, expected {legacy.BestScore}/{legacy.BestStars}.",
                    legacy.UserId,
                    legacy.ChallengeId,
                    issueModuleId,
                    legacy.FirstLegacyAttemptId,
                    null));
            }
        }

        var adaptiveAttempts = await dbContext.StudentChallengeAttempts.AsNoTracking()
            .Where(attempt => challengeIds.Contains(attempt.ChallengeId) && studentIds.Contains(attempt.StudentId))
            .Select(attempt => new
            {
                attempt.Id,
                attempt.StudentId,
                attempt.ChallengeId,
                attempt.CompletionStatus,
                ItemCount = dbContext.StudentChallengeItemAttempts.AsNoTracking()
                    .Count(item => item.StudentChallengeAttemptId == attempt.Id)
            })
            .ToListAsync(cancellationToken);

        foreach (var adaptive in adaptiveAttempts)
        {
            if (!string.Equals(adaptive.CompletionStatus, "completed", StringComparison.OrdinalIgnoreCase) || adaptive.ItemCount > 0)
                continue;

            moduleByChallenge.TryGetValue(adaptive.ChallengeId, out var issueModuleId);
            issues.Add(new AttemptConsistencyIssueDto(
                "critical",
                "missing_adaptive_item_telemetry",
                "An adaptive attempt is completed but has no item telemetry.",
                adaptive.StudentId,
                adaptive.ChallengeId,
                issueModuleId,
                null,
                adaptive.Id));
        }

        var itemTelemetry = await dbContext.StudentChallengeItemAttempts.AsNoTracking()
            .Include(item => item.StudentChallengeAttempt)
            .Where(item =>
                item.VocabularyItemId.HasValue &&
                challengeIds.Contains(item.StudentChallengeAttempt.ChallengeId) &&
                studentIds.Contains(item.StudentChallengeAttempt.StudentId))
            .Select(item => new
            {
                item.StudentChallengeAttemptId,
                item.VocabularyItemId,
                item.StudentChallengeAttempt.StudentId,
                item.StudentChallengeAttempt.ChallengeId
            })
            .ToListAsync(cancellationToken);

        foreach (var item in itemTelemetry)
        {
            var hasMastery = await dbContext.StudentWordMasteries.AsNoTracking()
                .AnyAsync(mastery =>
                    mastery.StudentId == item.StudentId &&
                    mastery.VocabularyItemId == item.VocabularyItemId!.Value,
                    cancellationToken);

            if (hasMastery)
                continue;

            moduleByChallenge.TryGetValue(item.ChallengeId, out var issueModuleId);
            issues.Add(new AttemptConsistencyIssueDto(
                "critical",
                "missing_word_mastery",
                "Item telemetry exists for a vocabulary item but StudentWordMastery is missing.",
                item.StudentId,
                item.ChallengeId,
                issueModuleId,
                null,
                item.StudentChallengeAttemptId));
        }

        return new AttemptConsistencyReportDto(
            classroomId,
            moduleId,
            studentId,
            challengeId,
            issues.Count,
            issues,
            DateTime.UtcNow);
    }
}
