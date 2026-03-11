using CleanArc.Domain.Entities.Progression;

namespace CleanArc.Application.Contracts.Persistence;

public interface IProgressionRepository
{
  // Levels
  Task<List<Level>> GetAllLevelsAsync();
  Task<Level> GetLevelByNumberAsync(int levelNumber);
  Task<Level> CreateLevelAsync(Level level);
  Task UpdateLevelAsync(Level level);
  Task DeleteLevelAsync(int levelId);

  // User progress
  Task<UserProgress> GetUserProgressAsync(int userId);
  Task<UserProgress> GetOrCreateUserProgressAsync(int userId);
  Task UpdateUserProgressAsync(UserProgress progress);
  Task AddXpAsync(int userId, int xpAmount);

  /// <summary>Adds diamonds to the user's account (User.Diamonds column).</summary>
  Task AddDiamondsAsync(int userId, int amount);
}
