using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Adaptive;

public class WordProgress : BaseEntity<int>
{
    public int StudentId { get; set; }
    public User.User Student { get; set; } = null!;

    public int WordId { get; set; }
    public VocabularyItem Word { get; set; } = null!;

    public int TotalAttempts { get; set; }
    public int TotalCorrect { get; set; }

    public int MasteryScore { get; set; }

    public DateTime? LastPracticedAt { get; set; }
    public DateTime? NextReviewDate { get; set; }
}
