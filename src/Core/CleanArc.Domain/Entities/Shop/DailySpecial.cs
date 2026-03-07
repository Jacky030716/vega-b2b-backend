using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Shop;

public class DailySpecial : BaseEntity<int>
{
  public int ShopItemId { get; set; }
  public int DiscountPercent { get; set; }
  public DateOnly ActiveDate { get; set; }

  #region Navigation Properties

  public ShopItem ShopItem { get; set; }

  #endregion
}
