using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Adaptive;

public class VocabularySyllableInfo : BaseEntity<int>
{
    public int VocabularyItemId { get; set; }
    public virtual VocabularyItem VocabularyItem { get; set; } = null!;
    public string SyllablesJson { get; set; } = "[]"; // e.g. ["bu", "ku"]
    public string? SyllableText { get; set; } // e.g. "bu-ku"
}
