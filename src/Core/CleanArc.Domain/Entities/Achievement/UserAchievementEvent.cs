using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Achievement;

/// <summary>
/// Idempotent event inbox for achievement processing.
/// </summary>
public class UserAchievementEvent : BaseEntity<int>
{
  public int UserId { get; set; }
  public string EventType { get; set; } = string.Empty;
  public string EventId { get; set; } = string.Empty;
  public string PropertiesJson { get; set; } = "{}";
  public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

  #region Navigation Properties
  public User.User User { get; set; } = null!;
  #endregion
}
