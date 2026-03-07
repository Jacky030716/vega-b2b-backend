using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Achievement;

public class UserBadge : BaseEntity<int>
{
  public int UserId { get; set; }
  public int BadgeId { get; set; }
  public int Progress { get; set; }
  public bool IsUnlocked { get; set; }
  public DateTime? UnlockedDate { get; set; }

  #region Navigation Properties

  public User.User User { get; set; }
  public Badge Badge { get; set; }

  #endregion
}
