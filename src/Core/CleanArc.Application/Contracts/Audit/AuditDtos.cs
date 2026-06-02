namespace CleanArc.Application.Contracts.Audit;

public static class AuditHealthStatuses
{
    public const string Healthy = "HEALTHY";
    public const string NeedsReview = "NEEDS_REVIEW";
    public const string NotStarted = "NOT_STARTED";
}

public record ClassroomHealthDto(
    int ClassroomId,
    int StudentCount,
    int WeakWordCount,
    int OverdueReviewCount,
    decimal AverageMasteryScore,
    int ModulesNeedingReviewCount,
    string Status);

public record StudentPerformanceAuditDto(
    int StudentId,
    string? StudentName,
    int WeakWordCount,
    int OverdueReviewCount,
    decimal AverageMasteryScore,
    IReadOnlyList<string> WeakWords,
    int AttemptCount,
    int CompletedChallengeCount);

public record ModuleHealthDto(
    int ClassroomId,
    int ModuleId,
    int ProgressPercent,
    int WeakWordCount,
    decimal AverageScore,
    int ChallengeCount,
    int CompletedChallengeCount,
    string Status);

public record WeakWordsAuditDto(
    int ClassroomId,
    int? ModuleId,
    IReadOnlyList<string> WeakWords,
    int AffectedStudents);
