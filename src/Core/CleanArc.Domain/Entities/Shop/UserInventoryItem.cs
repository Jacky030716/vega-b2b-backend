using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Shop;

public class UserInventoryItem : BaseEntity<int>
{
  public int UserId { get; set; }
  public int ShopItemId { get; set; }
  public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;

  #region Navigation Properties

  public User.User User { get; set; }
  public ShopItem ShopItem { get; set; }

  #endregion
}
