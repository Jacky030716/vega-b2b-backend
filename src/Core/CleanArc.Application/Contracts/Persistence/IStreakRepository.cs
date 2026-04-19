using CleanArc.Domain.Entities.Streak;

namespace CleanArc.Application.Contracts.Persistence;

public interface IStreakRepository
{
  Task<UserStreak> GetUserStreakAsync(int userId);
  Task<UserStreak> GetOrCreateUserStreakAsync(int userId);
  Task<DailyCheckIn> GetCheckInAsync(int userId, DateOnly date);
  Task<List<DailyCheckIn>> GetCheckInsForRangeAsync(int userId, DateOnly from, DateOnly to);
  Task AddCheckInAsync(DailyCheckIn checkIn);
  Task UpdateStreakAsync(UserStreak streak);
  Task<bool> HasClaimedMysteryRewardForDateAsync(int userId, DateOnly claimDate);
  Task MarkMysteryRewardClaimedAsync(UserStreak streak, DateOnly claimDate);
}
