using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Quiz;

namespace CleanArc.Domain.Entities.Classroom;

public class Classroom : BaseEntity<int>
{
  public string Name { get; set; }
  public string Description { get; set; }
  public string Subject { get; set; }
  public int YearLevel { get; set; } = 1;
  public string Thumbnail { get; set; }
  public string JoinCode { get; set; }
  public int TeacherId { get; set; }
  public bool IsActive { get; set; } = true;

  #region Navigation Properties

  public User.User Teacher { get; set; }
  public ICollection<ClassroomStudent> Students { get; set; } = new List<ClassroomStudent>();
  public ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
  public ICollection<CustomModule> CustomModules { get; set; } = new List<CustomModule>();

  #endregion
}
