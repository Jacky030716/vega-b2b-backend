using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Achievement;

/// <summary>
/// Join table tracking which badges a user has earned, and their featured display slots.
/// </summary>
public class UserBadge : BaseEntity<int>
{
  public int UserId { get; set; }
  public int BadgeId { get; set; }
  public DateTime EarnedAt { get; set; }

  /// <summary>Whether this badge is pinned to the user's public profile.</summary>
  public bool IsFeatured { get; set; }

  /// <summary>Display slot index (0, 1, 2) for the three featured badge slots.</summary>
  public int? SlotIndex { get; set; }

  #region Navigation Properties
  public User.User User { get; set; }
  public Badge Badge { get; set; }
  #endregion
}
