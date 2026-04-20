#nullable enable

using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Sticker;

public class StickerInventoryItem : BaseEntity<int>
{
  public int OwnerUserId { get; set; }
  public int CreatorUserId { get; set; }
  public int? SourceStickerId { get; set; }

  public string ImageUrl { get; set; } = string.Empty;
  public StickerOwnershipSource OwnershipSource { get; set; } = StickerOwnershipSource.Generated;
  public string PromptChoicesJson { get; set; } = "{}";
  public string? GenerationModel { get; set; }
  public DateTime? GeneratedAtUtc { get; set; }

  #region Navigation Properties

  public User.User OwnerUser { get; set; } = null!;
  public User.User CreatorUser { get; set; } = null!;
  public StickerInventoryItem? SourceSticker { get; set; }
  public ICollection<StickerInventoryItem> ClonedStickers { get; set; } = new List<StickerInventoryItem>();
  public ICollection<StickerGiftTransaction> SourceGiftTransactions { get; set; } = new List<StickerGiftTransaction>();

  #endregion
}
