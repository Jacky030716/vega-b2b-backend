using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Domain.Entities.Adaptive;

public class StudentWordMastery : BaseEntity<int>
{
    public int StudentId { get; set; }
    public User.User Student { get; set; } = null!;

    public int VocabularyItemId { get; set; }
    public VocabularyItem VocabularyItem { get; set; } = null!;

    public int? ModuleId { get; set; }
    public SyllabusModule? Module { get; set; }

    public int MasteryScore { get; set; }
    public string MasteryLevel { get; set; } = "NEW";
    public int TotalAttempts { get; set; }
    public int CorrectAttempts { get; set; }
    public int FirstTryCorrectCount { get; set; }
    public int? AverageResponseTimeMs { get; set; }
    public int TotalHintsUsed { get; set; }
    public int TotalRetries { get; set; }
    public DateTime? LastPracticedAt { get; set; }
    public DateTime? NextReviewAt { get; set; }
    public string WeaknessTagsJson { get; set; } = "[]";
    public int? LastGameTemplateId { get; set; }
    public GameTemplate? LastGameTemplate { get; set; }
}
