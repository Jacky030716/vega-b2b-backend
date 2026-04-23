using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Quiz;

namespace CleanArc.Domain.Entities.Adaptive;

public class ChallengeItem : BaseEntity<int>
{
    public int ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;

    public int? VocabularyItemId { get; set; }
    public VocabularyItem? VocabularyItem { get; set; }

    public int SequenceNo { get; set; }
    public string SettingsJson { get; set; } = "{}";
}
