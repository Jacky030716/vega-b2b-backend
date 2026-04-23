using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Adaptive;

public class VocabularyItem : BaseEntity<int>
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public int ModuleId { get; set; }
    public SyllabusModule Module { get; set; } = null!;

    public string Word { get; set; } = string.Empty;
    public string NormalizedWord { get; set; } = string.Empty;
    public string BmText { get; set; } = string.Empty;
    public string? ZhText { get; set; }
    public string? EnText { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int YearLevel { get; set; }
    public string SyllablesJson { get; set; } = "[]";
    public string? SyllableText { get; set; }
    public string ItemType { get; set; } = "WORD";
    public int DisplayOrder { get; set; }
    public string? PhoneticHint { get; set; }
    public string? PronunciationText { get; set; }
    public int DifficultyLevel { get; set; } = 1;
    public string? MeaningText { get; set; }
    public string? ExampleSentence { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
