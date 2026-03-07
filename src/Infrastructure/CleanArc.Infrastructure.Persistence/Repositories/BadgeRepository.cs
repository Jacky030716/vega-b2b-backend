using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Achievement;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class BadgeRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<Badge>(dbContext), IBadgeRepository
{
  public async Task<List<Badge>> GetAllBadgesAsync()
  {
    return await TableNoTracking.Where(b => b.IsActive).ToListAsync();
  }

  public async Task<Badge> GetBadgeByIdAsync(int badgeId)
  {
    return await TableNoTracking.FirstOrDefaultAsync(b => b.Id == badgeId);
  }

  public async Task<Badge> CreateBadgeAsync(Badge badge)
  {
    await AddAsync(badge);
    await DbContext.SaveChangesAsync();
    return badge;
  }

  public async Task UpdateBadgeAsync(Badge badge)
  {
    DbContext.Badges.Update(badge);
    await DbContext.SaveChangesAsync();
  }

  public async Task DeleteBadgeAsync(int badgeId)
  {
    await DeleteAsync(b => b.Id == badgeId);
  }

  // User badges
  public async Task<List<UserBadge>> GetUserBadgesAsync(int userId)
  {
    return await DbContext.UserBadges.AsNoTracking()
        .Include(ub => ub.Badge)
        .Where(ub => ub.UserId == userId)
        .ToListAsync();
  }

  public async Task<UserBadge> GetUserBadgeAsync(int userId, int badgeId)
  {
    return await DbContext.UserBadges
        .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
  }

  public async Task<UserBadge> CreateOrUpdateUserBadgeAsync(UserBadge userBadge)
  {
    var existing = await DbContext.UserBadges
        .FirstOrDefaultAsync(ub => ub.UserId == userBadge.UserId && ub.BadgeId == userBadge.BadgeId);

    if (existing == null)
    {
      DbContext.UserBadges.Add(userBadge);
    }
    else
    {
      existing.Progress = userBadge.Progress;
      existing.IsUnlocked = userBadge.IsUnlocked;
      existing.UnlockedDate = userBadge.UnlockedDate;
    }

    await DbContext.SaveChangesAsync();
    return existing ?? userBadge;
  }

  // Featured badges
  public async Task<List<FeaturedBadge>> GetFeaturedBadgesAsync(int userId)
  {
    return await DbContext.FeaturedBadges.AsNoTracking()
        .Include(fb => fb.Badge)
        .Where(fb => fb.UserId == userId)
        .OrderBy(fb => fb.SlotIndex)
        .ToListAsync();
  }

  public async Task SetFeaturedBadgesAsync(int userId, List<FeaturedBadge> featured)
  {
    var existing = await DbContext.FeaturedBadges.Where(fb => fb.UserId == userId).ToListAsync();
    DbContext.FeaturedBadges.RemoveRange(existing);
    DbContext.FeaturedBadges.AddRange(featured);
    await DbContext.SaveChangesAsync();
  }

  public async Task SetFeaturedBadgeAsync(int userId, int badgeId, int slotIndex)
  {
    var existing = await DbContext.FeaturedBadges.FirstOrDefaultAsync(fb => fb.UserId == userId && fb.SlotIndex == slotIndex);
    if (existing != null)
    {
      existing.BadgeId = badgeId;
    }
    else
    {
      DbContext.FeaturedBadges.Add(new FeaturedBadge { UserId = userId, BadgeId = badgeId, SlotIndex = slotIndex });
    }
    await DbContext.SaveChangesAsync();
  }
}
