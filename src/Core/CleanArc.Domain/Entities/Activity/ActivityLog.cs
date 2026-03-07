using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Activity;

public class ActivityLog : BaseEntity<int>
{
  public int UserId { get; set; }
  public string Type { get; set; } // quiz, achievement, streak, purchase, level_up
  public string Title { get; set; }
  public string Description { get; set; }
  public int? PointsEarned { get; set; }
  public string ReferenceId { get; set; } // optional FK to related entity

  #region Navigation Properties

  public User.User User { get; set; }

  #endregion
}
