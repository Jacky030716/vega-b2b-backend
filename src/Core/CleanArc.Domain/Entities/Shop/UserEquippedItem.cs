using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Shop;

public class UserEquippedItem : BaseEntity<int>
{
  public int UserId { get; set; }
  public string Category { get; set; } // hats, outfits, colors, props
  public int ShopItemId { get; set; }

  #region Navigation Properties

  public User.User User { get; set; }
  public ShopItem ShopItem { get; set; }

  #endregion
}
