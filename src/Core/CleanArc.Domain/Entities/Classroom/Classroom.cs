using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Classroom;

public class Classroom : BaseEntity<int>
{
  public string Name { get; set; }
  public string Description { get; set; }
  public string Subject { get; set; }
  public string Thumbnail { get; set; }
  public string JoinCode { get; set; }
  public int TeacherId { get; set; }
  public bool IsActive { get; set; } = true;

  #region Navigation Properties

  public User.User Teacher { get; set; }
  public ICollection<ClassroomStudent> Students { get; set; } = new List<ClassroomStudent>();
  public ICollection<ClassroomQuiz> Quizzes { get; set; } = new List<ClassroomQuiz>();

  #endregion
}
