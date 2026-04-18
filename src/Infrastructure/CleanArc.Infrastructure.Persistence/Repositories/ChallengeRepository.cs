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

    public async Task<int> GetNextOrderIndexForGameAsync(int gameId)
    {
        var currentMax = await DbContext.Challenges.AsNoTracking()
            .Where(c => c.GameId == gameId)
            .Select(c => (int?)c.OrderIndex)
            .MaxAsync();

        return (currentMax ?? 0) + 1;
    }

    public async Task<Challenge?> GetChallengeByIdAsync(int challengeId)
        => await DbContext.Challenges.AsNoTracking()
            .Include(c => c.Game)
            .FirstOrDefaultAsync(c => c.Id == challengeId);

    public async Task<int> CountChallengesCreatedByTeacherAsync(int teacherId)
        => await DbContext.Challenges.AsNoTracking()
            .CountAsync(c => c.CreatedById == teacherId);

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

    // ── Challenge Progress (leaderboard aggregates) ───────────────────────────

    public async Task UpsertChallengeProgressAsync(ChallengeProgress incoming)
    {
        var existing = await DbContext.ChallengeProgresses
            .FirstOrDefaultAsync(cp =>
                cp.UserId == incoming.UserId &&
                cp.ChallengeId == incoming.ChallengeId &&
                cp.ClassroomId == incoming.ClassroomId);

        if (existing is null)
        {
            DbContext.ChallengeProgresses.Add(incoming);
        }
        else
        {
            existing.AttemptCount = incoming.AttemptCount;
            existing.HasCompleted = incoming.HasCompleted;
            existing.BestScore = incoming.BestScore;
            existing.BestStars = incoming.BestStars;
            existing.BestAccuracy = incoming.BestAccuracy;
            existing.BestDurationSeconds = incoming.BestDurationSeconds;
            existing.TotalXpEarned = incoming.TotalXpEarned;
            existing.LastAttemptAt = incoming.LastAttemptAt;
            existing.FirstCompletedAt = incoming.FirstCompletedAt;
        }

        await DbContext.SaveChangesAsync();
    }

    public async Task<List<ChallengeProgress>> GetChallengeLeaderboardAsync(int challengeId, int classroomId)
        => await DbContext.ChallengeProgresses.AsNoTracking()
            .Include(cp => cp.User)
            .Where(cp => cp.ChallengeId == challengeId && cp.ClassroomId == classroomId)
            .OrderByDescending(cp => cp.BestScore)
            .ThenBy(cp => cp.BestDurationSeconds)
            .ThenBy(cp => cp.AttemptCount)
            .ToListAsync();

    public async Task<ChallengeProgress?> GetStudentChallengeProgressAsync(int userId, int challengeId, int classroomId)
        => await DbContext.ChallengeProgresses.AsNoTracking()
            .FirstOrDefaultAsync(cp =>
                cp.UserId == userId &&
                cp.ChallengeId == challengeId &&
                cp.ClassroomId == classroomId);
}
