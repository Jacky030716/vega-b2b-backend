using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Classroom;

namespace CleanArc.Domain.Entities.Adaptive;

public class SyllabusModule : BaseEntity<int>
{
    public const string PredefinedModuleType = "PREDEFINED";
    public const string CustomModuleType = "CUSTOM";

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
    public string ModuleType { get; set; } = PredefinedModuleType;
    public string SourceType { get; set; } = "predefined";
    public int? CreatedByTeacherId { get; set; }
    public User.User? CreatedByTeacher { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<VocabularyItem> VocabularyItems { get; set; } = new List<VocabularyItem>();
    public ICollection<ClassroomModule> ClassroomModules { get; set; } = new List<ClassroomModule>();
}
