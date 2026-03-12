using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Achievement;

/// <summary>
/// Represents an achievement badge that can be earned by a student.
/// ImageRef stores a Firebase Storage relative path (e.g. "badges/7_day_streak.png").
/// The frontend resolves this to a download URL via resolveStorageRefToUrl().
/// </summary>
public class Badge : BaseEntity<int>
{
  public string Name { get; set; }
  public string Description { get; set; }

  /// <summary>Firebase Storage relative path, e.g. "badges/perfect_score.png"</summary>
  public string ImageRef { get; set; }

  /// <summary>streak | quiz | milestone | social | secret</summary>
  public string Category { get; set; }

  /// <summary>wood | silver | gold | crystal</summary>
  public string Rarity { get; set; }

  /// <summary>Human-readable unlock requirement shown to the user.</summary>
  public string Requirement { get; set; }

  public bool IsSecret { get; set; }

  #region Navigation Properties
  public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
  #endregion
}
