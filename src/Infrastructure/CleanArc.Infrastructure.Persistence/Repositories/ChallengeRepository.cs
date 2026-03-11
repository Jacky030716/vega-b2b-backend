using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class ChallengeRepository(ApplicationDbContext dbContext)
    : BaseAsyncRepository<Challenge>(dbContext), IChallengeRepository
{
  // ── Games ────────────────────────────────────────────────────────────────

  public async Task<List<Game>> GetAllGamesAsync()
      => await DbContext.Games.AsNoTracking()
          .OrderBy(g => g.Id)
          .ToListAsync();

  public async Task<Game?> GetGameByKeyAsync(string gameKey)
      => await DbContext.Games.AsNoTracking()
          .FirstOrDefaultAsync(g => g.Key == gameKey);

  // ── Challenges ────────────────────────────────────────────────────────────

  public async Task<List<Challenge>> GetChallengesForGameAsync(int gameId)
      => await DbContext.Challenges.AsNoTracking()
          .Where(c => c.GameId == gameId)
          .OrderBy(c => c.OrderIndex)
          .ThenBy(c => c.DifficultyLevel)
          .ToListAsync();

  public async Task<Challenge?> GetChallengeByIdAsync(int challengeId)
      => await DbContext.Challenges.AsNoTracking()
          .Include(c => c.Game)
          .FirstOrDefaultAsync(c => c.Id == challengeId);

  public async Task<Challenge> CreateChallengeAsync(Challenge challenge)
  {
    DbContext.Challenges.Add(challenge);
    await DbContext.SaveChangesAsync();
    return challenge;
  }

  public async Task UpdateChallengeAsync(Challenge challenge)
  {
    DbContext.Challenges.Update(challenge);
    await DbContext.SaveChangesAsync();
  }

  // ── Attempts ─────────────────────────────────────────────────────────────

  public async Task<Attempt> CreateAttemptAsync(Attempt attempt)
  {
    DbContext.Attempts.Add(attempt);
    await DbContext.SaveChangesAsync();
    return attempt;
  }

  public async Task<Attempt?> GetAttemptByIdAsync(int attemptId)
      => await DbContext.Attempts.AsNoTracking()
          .FirstOrDefaultAsync(a => a.Id == attemptId);

  public async Task UpdateAttemptAsync(Attempt attempt)
  {
    DbContext.Attempts.Update(attempt);
    await DbContext.SaveChangesAsync();
  }

  public async Task<Attempt?> GetPriorCompletedAttemptForChallengeAsync(int userId, int challengeId, int excludeAttemptId)
      => await DbContext.Attempts.AsNoTracking()
          .Where(a => a.UserId == userId
                   && a.ChallengeId == challengeId
                   && a.IsCompleted
                   && a.Id != excludeAttemptId)
          .FirstOrDefaultAsync();

  public async Task<List<Attempt>> GetUserBestAttemptsForGameAsync(int userId, int gameId)
  {
    // Get all challenge IDs for this game
    var challengeIds = await DbContext.Challenges.AsNoTracking()
        .Where(c => c.GameId == gameId)
        .Select(c => c.Id)
        .ToListAsync();

    // For each challenge, return the best (highest score) completed attempt
    return await DbContext.Attempts.AsNoTracking()
        .Where(a => a.UserId == userId
                 && challengeIds.Contains(a.ChallengeId)
                 && a.IsCompleted)
        .GroupBy(a => a.ChallengeId)
        .Select(g => g.OrderByDescending(a => a.Score).First())
        .ToListAsync();
  }
}
