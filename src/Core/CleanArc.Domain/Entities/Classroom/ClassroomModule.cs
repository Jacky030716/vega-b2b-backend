using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Adaptive;

namespace CleanArc.Domain.Entities.Classroom;

public class ClassroomModule : BaseEntity<int>
{
    public int ClassroomId { get; set; }
    public Classroom Classroom { get; set; } = null!;
    public int ModuleId { get; set; }
    public SyllabusModule Module { get; set; } = null!;
}
