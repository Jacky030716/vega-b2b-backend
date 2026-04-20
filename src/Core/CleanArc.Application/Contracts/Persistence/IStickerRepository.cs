#nullable enable

using CleanArc.Domain.Entities.Sticker;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Application.Contracts.Persistence;

public interface IStickerRepository
{
  Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken);

  Task<StickerInventoryItem?> GetStickerByIdAsync(int stickerId, CancellationToken cancellationToken);
  Task<StickerInventoryItem?> GetStickerForOwnerAsync(int stickerId, int ownerUserId, CancellationToken cancellationToken);
  Task<List<StickerInventoryItem>> GetInventoryByOwnerAsync(int ownerUserId, CancellationToken cancellationToken);
  Task AddStickerAsync(StickerInventoryItem sticker, CancellationToken cancellationToken);

  Task<StickerGiftTransaction?> GetGiftByIdAsync(int giftId, CancellationToken cancellationToken);
  Task<List<StickerGiftTransaction>> GetUnclaimedGiftsByRecipientAsync(int recipientUserId, CancellationToken cancellationToken);
  Task AddGiftAsync(StickerGiftTransaction gift, CancellationToken cancellationToken);
}
