using CleanArc.Domain.Entities.Achievement;

namespace CleanArc.Application.Contracts.Persistence;

public interface IBadgeRepository
{
  /// <summary>Returns all badge definitions (the catalog).</summary>
  Task<List<Badge>> GetAllBadgesAsync();

  /// <summary>Returns a single badge by ID.</summary>
  Task<Badge?> GetBadgeByIdAsync(int badgeId);

  /// <summary>Returns all badges a specific user has earned.</summary>
  Task<List<UserBadge>> GetUserBadgesAsync(int userId);

  /// <summary>Returns all progress rows for a specific user across badges.</summary>
  Task<List<UserBadgeProgress>> GetUserBadgeProgressesAsync(int userId);

  /// <summary>Returns all achievement triggers.</summary>
  Task<List<AchievementTrigger>> GetAchievementTriggersAsync();

  /// <summary>Returns a specific UserBadge record.</summary>
  Task<UserBadge?> GetUserBadgeAsync(int userId, int badgeId);

  /// <summary>Awards a badge to a user (creates UserBadge record).</summary>
  Task<UserBadge> AwardBadgeAsync(int userId, int badgeId);

  /// <summary>
  /// Sets a badge as featured at a given slot (0-2).
  /// Unfeaturing the existing badge in that slot before pinning the new one.
  /// </summary>
  Task SetFeaturedBadgeAsync(int userId, int badgeId, int slotIndex);
}
