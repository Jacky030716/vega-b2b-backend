using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Domain.Entities.Adaptive;

public class StudentChallengeAttempt : BaseEntity<int>
{
    public int ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;

    public int StudentId { get; set; }
    public User.User Student { get; set; } = null!;

    public int AttemptNo { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TotalScore { get; set; }
    public string CompletionStatus { get; set; } = "started";
    public int? AverageResponseTimeMs { get; set; }
    public int TotalHintsUsed { get; set; }
    public int TotalRetries { get; set; }
    public string? DeviceInfo { get; set; }

    public ICollection<StudentChallengeItemAttempt> ItemAttempts { get; set; } = new List<StudentChallengeItemAttempt>();
}
