using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Streak;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class StreakRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<UserStreak>(dbContext), IStreakRepository
{
  public async Task<UserStreak> GetUserStreakAsync(int userId)
  {
    return await TableNoTracking.FirstOrDefaultAsync(s => s.UserId == userId);
  }

  public async Task<UserStreak> GetOrCreateUserStreakAsync(int userId)
  {
    var streak = await DbContext.UserStreaks.FirstOrDefaultAsync(s => s.UserId == userId);
    if (streak == null)
    {
      streak = new UserStreak { UserId = userId, CurrentStreak = 0, BestStreak = 0 };
      DbContext.UserStreaks.Add(streak);
      await DbContext.SaveChangesAsync();
    }
    return streak;
  }

  public async Task<DailyCheckIn> GetCheckInAsync(int userId, DateOnly date)
  {
    return await DbContext.DailyCheckIns.AsNoTracking()
        .FirstOrDefaultAsync(c => c.UserId == userId && c.CheckInDate == date);
  }

  public async Task<List<DailyCheckIn>> GetCheckInsForRangeAsync(int userId, DateOnly from, DateOnly to)
  {
    return await DbContext.DailyCheckIns.AsNoTracking()
        .Where(c => c.UserId == userId && c.CheckInDate >= from && c.CheckInDate <= to)
        .OrderBy(c => c.CheckInDate)
        .ToListAsync();
  }

  public async Task AddCheckInAsync(DailyCheckIn checkIn)
  {
    DbContext.DailyCheckIns.Add(checkIn);
    await DbContext.SaveChangesAsync();
  }

  public async Task UpdateStreakAsync(UserStreak streak)
  {
    DbContext.UserStreaks.Update(streak);
    await DbContext.SaveChangesAsync();
  }

  public async Task<bool> HasClaimedMysteryRewardForDateAsync(int userId, DateOnly claimDate)
  {
    var streak = await DbContext.UserStreaks.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
    return streak?.LastMysteryRewardClaimedDate == claimDate;
  }

  public async Task MarkMysteryRewardClaimedAsync(UserStreak streak, DateOnly claimDate)
  {
    streak.LastMysteryRewardClaimedDate = claimDate;
    DbContext.UserStreaks.Update(streak);
    await DbContext.SaveChangesAsync();
  }
}
