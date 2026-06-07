using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Adaptive;

public class VocabularyTranslation : BaseEntity<int>
{
    public int VocabularyItemId { get; set; }
    public virtual VocabularyItem VocabularyItem { get; set; } = null!;
    public string LanguageCode { get; set; } = string.Empty; // e.g. "ms", "en", "zh"
    public string TranslationText { get; set; } = string.Empty;
}
