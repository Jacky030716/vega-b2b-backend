using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Adaptive;

public class SyllabusModule : BaseEntity<int>
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string ModuleCode { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int YearLevel { get; set; }
    public string Term { get; set; } = string.Empty;
    public int? Week { get; set; }
    public int? UnitNumber { get; set; }
    public string UnitTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceType { get; set; } = "predefined";
    public bool IsActive { get; set; } = true;

    public ICollection<VocabularyItem> VocabularyItems { get; set; } = new List<VocabularyItem>();
}
