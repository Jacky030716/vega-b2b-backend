using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Achievement;

/// <summary>
/// Stores incremental progress toward a badge for a specific user.
/// </summary>
public class UserBadgeProgress : BaseEntity<int>
{
  public int UserId { get; set; }
  public int BadgeId { get; set; }

  /// <summary>
  /// Numeric progress value used by dynamic rule aggregations.
  /// </summary>
  public decimal ProgressValue { get; set; }

  public DateTime LastEvaluatedAt { get; set; } = DateTime.UtcNow;

  #region Navigation Properties
  public User.User User { get; set; } = null!;
  public Badge Badge { get; set; } = null!;
  #endregion
}
