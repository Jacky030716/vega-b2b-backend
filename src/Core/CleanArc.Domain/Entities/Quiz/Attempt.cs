using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Domain.Entities.Quiz;

public class Attempt : BaseEntity<int>
{
    public int UserId { get; set; }
    public User.User User { get; set; } = null!;

    public int ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;

    public int Score { get; set; }
    public int CoinsEarned { get; set; }
    public int XPEarned { get; set; }

    // 0–3 stars earned on this attempt
    public int StarsEarned { get; set; } = 0;

    // Whether the attempt was completed (vs. abandoned)
    public bool IsCompleted { get; set; } = false;

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    // JSON string containing what the user answered, used for analytics
    public string AttemptData { get; set; } = string.Empty;
}
