using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Classroom;

public class ClassroomQuiz : BaseEntity<int>
{
  public int ClassroomId { get; set; }
  public string QuizId { get; set; } // matches Quiz.QuizId (string business key)
  public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
  public DateTime? DueDate { get; set; }

  #region Navigation Properties

  public Classroom Classroom { get; set; }

  #endregion
}
