using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Quiz;

namespace CleanArc.Domain.Entities.Classroom;

public class CustomModule : BaseEntity<int>
{
    public int ClassroomId { get; set; }
    public Classroom Classroom { get; set; } = null!;
    public string Name { get; set; } = "Custom Module";
    public int YearLevel { get; set; } = 1;
    public int CreatedByTeacherId { get; set; }
    public User.User CreatedByTeacher { get; set; } = null!;
    public ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
}
