using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Progression;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class ProgressionRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<Level>(dbContext), IProgressionRepository
{
  // Levels
  public async Task<List<Level>> GetAllLevelsAsync()
  {
    return await TableNoTracking.OrderBy(l => l.LevelNumber).ToListAsync();
  }

  public async Task<Level> GetLevelByNumberAsync(int levelNumber)
  {
    return await TableNoTracking.FirstOrDefaultAsync(l => l.LevelNumber == levelNumber);
  }

  public async Task<Level> CreateLevelAsync(Level level)
  {
    await AddAsync(level);
    await DbContext.SaveChangesAsync();
    return level;
  }

  public async Task UpdateLevelAsync(Level level)
  {
    DbContext.Levels.Update(level);
    await DbContext.SaveChangesAsync();
  }

  public async Task DeleteLevelAsync(int levelId)
  {
    var level = await DbContext.Levels.FirstOrDefaultAsync(l => l.Id == levelId);
    if (level != null)
    {
      DbContext.Levels.Remove(level);
      await DbContext.SaveChangesAsync();
    }
  }

  // User Progress
  public async Task<UserProgress> GetUserProgressAsync(int userId)
  {
    return await DbContext.UserProgresses.AsNoTracking()
        .FirstOrDefaultAsync(up => up.UserId == userId);
  }

  public async Task<UserProgress> GetOrCreateUserProgressAsync(int userId)
  {
    var progress = await DbContext.UserProgresses.FirstOrDefaultAsync(up => up.UserId == userId);
    if (progress == null)
    {
      progress = new UserProgress
      {
        UserId = userId,
        TotalXP = 0,
        CurrentLevel = 1,
        TotalQuizzesTaken = 0,
        TotalCorrectAnswers = 0,
        TotalTimePlayed = 0
      };
      DbContext.UserProgresses.Add(progress);
      await DbContext.SaveChangesAsync();
    }
    return progress;
  }

  public async Task UpdateUserProgressAsync(UserProgress progress)
  {
    DbContext.UserProgresses.Update(progress);
    await DbContext.SaveChangesAsync();
  }

  public async Task AddXpAsync(int userId, int xpAmount)
  {
    var progress = await GetOrCreateUserProgressAsync(userId);
    progress.TotalXP += xpAmount;

    // Check for level up
    var nextLevel = await DbContext.Levels.AsNoTracking()
        .Where(l => l.LevelNumber == progress.CurrentLevel + 1 && l.RequiredXP <= progress.TotalXP)
        .FirstOrDefaultAsync();

    if (nextLevel != null)
    {
      progress.CurrentLevel = nextLevel.LevelNumber;
    }

    await DbContext.SaveChangesAsync();
  }
}
