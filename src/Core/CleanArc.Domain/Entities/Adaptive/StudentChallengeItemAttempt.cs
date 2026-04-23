using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Quiz;

namespace CleanArc.Domain.Entities.Adaptive;

public class StudentChallengeItemAttempt : BaseEntity<int>
{
    public int StudentChallengeAttemptId { get; set; }
    public StudentChallengeAttempt StudentChallengeAttempt { get; set; } = null!;

    public int ChallengeItemId { get; set; }
    public ChallengeItem ChallengeItem { get; set; } = null!;

    public int? VocabularyItemId { get; set; }
    public VocabularyItem? VocabularyItem { get; set; }

    public int? GameTemplateId { get; set; }
    public GameTemplate? GameTemplate { get; set; }

    public DateTime PresentedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnsweredAt { get; set; }
    public int? ResponseTimeMs { get; set; }
    public bool WasCorrect { get; set; }
    public bool FirstAttemptCorrect { get; set; }
    public int RetriesCount { get; set; }
    public int HintsUsed { get; set; }
    public string? AnswerText { get; set; }
    public string? ExpectedAnswerText { get; set; }
    public decimal? SpeechConfidence { get; set; }
    public string? ErrorType { get; set; }
    public string RawTelemetryJson { get; set; } = "{}";
}
