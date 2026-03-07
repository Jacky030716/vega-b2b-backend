using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Shop;

public class DiamondTransaction : BaseEntity<int>
{
  public int UserId { get; set; }
  public int Amount { get; set; } // positive = earned, negative = spent
  public string Reason { get; set; } // purchase, quiz_reward, mission_reward, check_in, mystery_capsule
  public string ReferenceId { get; set; } // optional FK to item, mission, etc.

  #region Navigation Properties

  public User.User User { get; set; }

  #endregion
}
