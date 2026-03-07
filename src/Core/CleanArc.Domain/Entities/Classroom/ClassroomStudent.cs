using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Classroom;

public class ClassroomStudent : BaseEntity<int>
{
  public int ClassroomId { get; set; }
  public int UserId { get; set; }
  public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

  #region Navigation Properties

  public Classroom Classroom { get; set; }
  public User.User User { get; set; }

  #endregion
}
