using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Adaptive;

public class SpellingTest : BaseEntity<int>
{
    public int ClassroomId { get; set; }
    public CleanArc.Domain.Entities.Classroom.Classroom Classroom { get; set; } = null!;

    public string Subject { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SourceModuleIdsJson { get; set; } = "[]";
    public string WordItemIdsJson { get; set; } = "[]";
    public DateTime DueAt { get; set; }
    public string Status { get; set; } = SpellingTestStatuses.Active;

    public int CreatedByTeacherId { get; set; }
    public CleanArc.Domain.Entities.User.User CreatedByTeacher { get; set; } = null!;

    public string ConfigJson { get; set; } = "{}";
    public ICollection<StudentSpellingTestAttempt> StudentAttempts { get; set; } = new List<StudentSpellingTestAttempt>();
}

public class StudentSpellingTestAttempt : BaseEntity<int>
{
    public int SpellingTestId { get; set; }
    public SpellingTest SpellingTest { get; set; } = null!;

    public int StudentId { get; set; }
    public CleanArc.Domain.Entities.User.User Student { get; set; } = null!;

    public string Status { get; set; } = StudentSpellingTestAttemptStatuses.NotStarted;
    public int? Score { get; set; }
    public int? Stars { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? LastResumedAt { get; set; }
    public int? RemainingSeconds { get; set; }
    public DateTime? ModalSeenAt { get; set; }
    public DateTime? DismissedAt { get; set; }
    public string ResultJson { get; set; } = "{}";
}

public static class SpellingTestStatuses
{
    public const string Draft = "DRAFT";
    public const string Active = "ACTIVE";
    public const string Completed = "COMPLETED";
    public const string Archived = "ARCHIVED";
}

public static class StudentSpellingTestAttemptStatuses
{
    public const string NotStarted = "NOT_STARTED";
    public const string InProgress = "IN_PROGRESS";
    public const string Completed = "COMPLETED";
    public const string Overdue = "OVERDUE";
}
