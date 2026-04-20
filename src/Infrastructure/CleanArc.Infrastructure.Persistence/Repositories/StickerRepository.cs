#nullable enable

using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Sticker;
using CleanArc.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class StickerRepository(ApplicationDbContext dbContext) : IStickerRepository
{
  public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
  {
    return dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
  }

  public Task<StickerInventoryItem?> GetStickerByIdAsync(int stickerId, CancellationToken cancellationToken)
  {
    return dbContext.StickerInventoryItems
      .FirstOrDefaultAsync(s => s.Id == stickerId, cancellationToken);
  }

  public Task<StickerInventoryItem?> GetStickerForOwnerAsync(int stickerId, int ownerUserId, CancellationToken cancellationToken)
  {
    return dbContext.StickerInventoryItems
      .FirstOrDefaultAsync(s => s.Id == stickerId && s.OwnerUserId == ownerUserId, cancellationToken);
  }

  public Task<List<StickerInventoryItem>> GetInventoryByOwnerAsync(int ownerUserId, CancellationToken cancellationToken)
  {
    return dbContext.StickerInventoryItems
      .AsNoTracking()
      .Where(s => s.OwnerUserId == ownerUserId)
      .OrderByDescending(s => s.CreatedTime)
      .ToListAsync(cancellationToken);
  }

  public async Task AddStickerAsync(StickerInventoryItem sticker, CancellationToken cancellationToken)
  {
    await dbContext.StickerInventoryItems.AddAsync(sticker, cancellationToken);
  }

  public Task<StickerGiftTransaction?> GetGiftByIdAsync(int giftId, CancellationToken cancellationToken)
  {
    return dbContext.StickerGiftTransactions
      .Include(g => g.RecipientSticker)
      .FirstOrDefaultAsync(g => g.Id == giftId, cancellationToken);
  }

  public Task<List<StickerGiftTransaction>> GetUnclaimedGiftsByRecipientAsync(int recipientUserId, CancellationToken cancellationToken)
  {
    return dbContext.StickerGiftTransactions
      .AsNoTracking()
      .Include(g => g.SourceSticker)
      .Where(g => g.RecipientUserId == recipientUserId && g.Status == StickerGiftStatus.PendingClaim)
      .OrderByDescending(g => g.CreatedTime)
      .ToListAsync(cancellationToken);
  }

  public async Task AddGiftAsync(StickerGiftTransaction gift, CancellationToken cancellationToken)
  {
    await dbContext.StickerGiftTransactions.AddAsync(gift, cancellationToken);
  }
}
