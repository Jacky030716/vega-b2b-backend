using CleanArc.Domain.Entities.Achievement;

namespace CleanArc.Application.Contracts.Persistence;

public interface IBadgeRepository
{
  Task<List<Badge>> GetAllBadgesAsync();
  Task<Badge> GetBadgeByIdAsync(int badgeId);
  Task<Badge> CreateBadgeAsync(Badge badge);
  Task UpdateBadgeAsync(Badge badge);
  Task DeleteBadgeAsync(int badgeId);

  // User badges
  Task<List<UserBadge>> GetUserBadgesAsync(int userId);
  Task<UserBadge> GetUserBadgeAsync(int userId, int badgeId);
  Task<UserBadge> CreateOrUpdateUserBadgeAsync(UserBadge userBadge);

  // Featured badges
  Task<List<FeaturedBadge>> GetFeaturedBadgesAsync(int userId);
  Task SetFeaturedBadgesAsync(int userId, List<FeaturedBadge> featured);
  Task SetFeaturedBadgeAsync(int userId, int badgeId, int slotIndex);
}
