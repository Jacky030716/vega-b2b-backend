using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Domain.Entities.Adaptive;

public class StudentSkillProfile : BaseEntity<int>
{
    public int StudentId { get; set; }
    public User.User Student { get; set; } = null!;

    public string Subject { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int SpellingRecallScore { get; set; }
    public int PronunciationRecallScore { get; set; }
    public int SyllableAssemblyScore { get; set; }
    public int VisualMemoryScore { get; set; }
    public int AuditoryMemoryScore { get; set; }
}
