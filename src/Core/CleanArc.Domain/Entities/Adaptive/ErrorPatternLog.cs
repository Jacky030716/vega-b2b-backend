using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Domain.Entities.Adaptive;

public class ErrorPatternLog : BaseEntity<int>
{
    public int StudentId { get; set; }
    public User.User Student { get; set; } = null!;

    public int? VocabularyItemId { get; set; }
    public VocabularyItem? VocabularyItem { get; set; }

    public int? ChallengeItemAttemptId { get; set; }
    public StudentChallengeItemAttempt? ChallengeItemAttempt { get; set; }

    public string PatternType { get; set; } = string.Empty;
    public string? ObservedValue { get; set; }
    public string? ExpectedValue { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
