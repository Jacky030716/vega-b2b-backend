using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Achievement;

public class Badge : BaseEntity<int>
{
  public string Name { get; set; }
  public string Description { get; set; }
  public string BadgeImageUrl { get; set; }
  public string Rarity { get; set; } // wood, silver, gold, crystal
  public string Category { get; set; } // streak, milestone, quiz, social, secret
  public int MaxProgress { get; set; }
  public string UnlockCriteria { get; set; } // JSON — rule engine criteria
  public bool IsActive { get; set; } = true;

  #region Navigation Properties

  public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();

  #endregion
}
