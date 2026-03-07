using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Progression;

public class UserProgress : BaseEntity<int>
{
  public int UserId { get; set; }
  public int TotalXP { get; set; }
  public int CurrentLevel { get; set; } = 1;
  public int TotalQuizzesTaken { get; set; }
  public int TotalCorrectAnswers { get; set; }
  public int TotalTimePlayed { get; set; } // seconds

  #region Navigation Properties

  public User.User User { get; set; }

  #endregion
}
