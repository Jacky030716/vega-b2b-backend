using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Classroom;

public class LeaderboardEntry : BaseEntity<int>
{
  public string QuizId { get; set; }
  public int? ClassroomId { get; set; }
  public int UserId { get; set; }
  public int Score { get; set; }
  public int TotalPoints { get; set; }
  public double Percentage { get; set; }
  public int TimeSpent { get; set; } // seconds
  public DateTime CompletedAt { get; set; }

  #region Navigation Properties

  public User.User User { get; set; }
  public Classroom Classroom { get; set; }

  #endregion
}
