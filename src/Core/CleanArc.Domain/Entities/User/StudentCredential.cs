using CleanArc.Domain.Common;
namespace CleanArc.Domain.Entities.User;

public class StudentCredential : BaseEntity<int>
{
  public int UserId { get; set; }
  public int ClassroomId { get; set; }
  public string StudentLoginCode { get; set; } = string.Empty;
  public string VisualPasswordHash { get; set; } = string.Empty;
  public bool IsActive { get; set; } = true;
  public int FailedAttempts { get; set; }
  public DateTime? LastFailedAt { get; set; }
  public DateTime? LastSuccessfulLoginAt { get; set; }

  public User User { get; set; }
  public Domain.Entities.Classroom.Classroom Classroom { get; set; }
}
