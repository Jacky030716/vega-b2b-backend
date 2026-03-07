using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Shop;

public class ShopItem : BaseEntity<int>
{
  public string Name { get; set; }
  public string Description { get; set; }
  public string Category { get; set; } // hats, outfits, colors, props, avatar
  public int Price { get; set; }
  public string Currency { get; set; } = "diamonds"; // diamonds, coins
  public string ImageUrl { get; set; }
  public string Rarity { get; set; } // common, rare, epic, legendary
  public int? RequiredLevel { get; set; }
  public bool IsAvailable { get; set; } = true;
  public bool IsLimitedEdition { get; set; }
  public int? Stock { get; set; }

  #region Navigation Properties

  public ICollection<UserInventoryItem> UserInventoryItems { get; set; } = new List<UserInventoryItem>();

  #endregion
}
