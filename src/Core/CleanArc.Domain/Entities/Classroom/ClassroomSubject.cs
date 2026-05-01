using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Classroom;

public class ClassroomSubject : BaseEntity<int>
{
    public int ClassroomId { get; set; }
    public Classroom Classroom { get; set; } = null!;
    public string Subject { get; set; } = string.Empty;
}
