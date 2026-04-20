using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Sticker;

public class StickerGiftTransaction : BaseEntity<int>
{
  public int SenderUserId { get; set; }
  public int RecipientUserId { get; set; }
  public int SourceStickerId { get; set; }
  public int RecipientStickerId { get; set; }

  public int DiamondCost { get; set; } = 5;
  public StickerGiftStatus Status { get; set; } = StickerGiftStatus.PendingClaim;
  public DateTime? ClaimedAtUtc { get; set; }

  #region Navigation Properties

  public User.User SenderUser { get; set; } = null!;
  public User.User RecipientUser { get; set; } = null!;
  public StickerInventoryItem SourceSticker { get; set; } = null!;
  public StickerInventoryItem RecipientSticker { get; set; } = null!;

  #endregion
}
