using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Achievement;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class BadgeRepository(ApplicationDbContext dbContext)
    : BaseAsyncRepository<Badge>(dbContext), IBadgeRepository
{
  public async Task<List<Badge>> GetAllBadgesAsync()
  {
    return await TableNoTracking.OrderBy(b => b.Category).ThenBy(b => b.Id).ToListAsync();
  }

  public async Task<Badge?> GetBadgeByIdAsync(int badgeId)
  {
    return await TableNoTracking.FirstOrDefaultAsync(b => b.Id == badgeId);
  }

  public async Task<List<UserBadge>> GetUserBadgesAsync(int userId)
  {
    return await DbContext.UserBadges
        .AsNoTracking()
        .Include(ub => ub.Badge)
        .Where(ub => ub.UserId == userId)
        .OrderBy(ub => ub.EarnedAt)
        .ToListAsync();
  }

  public async Task<List<UserBadgeProgress>> GetUserBadgeProgressesAsync(int userId)
  {
    return await DbContext.UserBadgeProgresses
    .AsNoTracking()
    .Where(p => p.UserId == userId)
    .ToListAsync();
  }

  public async Task<List<AchievementTrigger>> GetAchievementTriggersAsync()
  {
    return await DbContext.AchievementTriggers
    .AsNoTracking()
    .Where(t => t.IsActive)
    .ToListAsync();
  }

  public async Task<UserBadge?> GetUserBadgeAsync(int userId, int badgeId)
  {
    return await DbContext.UserBadges
        .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
  }

  public async Task<UserBadge> AwardBadgeAsync(int userId, int badgeId)
  {
    var userBadge = new UserBadge
    {
      UserId = userId,
      BadgeId = badgeId,
      EarnedAt = DateTime.UtcNow,
      IsFeatured = false,
    };

    // Concurrency-safe idempotency:
    // unique index (UserId, BadgeId) guarantees one award; on conflict, load existing.
    try
    {
      await DbContext.UserBadges.AddAsync(userBadge);
      await DbContext.SaveChangesAsync();
      return userBadge;
    }
    catch (DbUpdateException)
    {
      var existing = await GetUserBadgeAsync(userId, badgeId);
      if (existing is not null)
      {
        return existing;
      }

      throw;
    }
  }

  public async Task SetFeaturedBadgeAsync(int userId, int badgeId, int slotIndex)
  {
    // Unpin any badge currently occupying this slot
    var existing = await DbContext.UserBadges
        .Where(ub => ub.UserId == userId && ub.IsFeatured && ub.SlotIndex == slotIndex)
        .FirstOrDefaultAsync();

    if (existing is not null)
    {
      existing.IsFeatured = false;
      existing.SlotIndex = null;
    }

    // Pin the requested badge
    var target = await GetUserBadgeAsync(userId, badgeId)
        ?? throw new InvalidOperationException("User has not earned this badge.");

    target.IsFeatured = true;
    target.SlotIndex = slotIndex;

    await DbContext.SaveChangesAsync();
  }
}
