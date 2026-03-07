using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Achievement;

public class FeaturedBadge : BaseEntity<int>
{
  public int UserId { get; set; }
  public int BadgeId { get; set; }
  public int SlotIndex { get; set; } // 0, 1, 2

  #region Navigation Properties

  public User.User User { get; set; }
  public Badge Badge { get; set; }

  #endregion
}
