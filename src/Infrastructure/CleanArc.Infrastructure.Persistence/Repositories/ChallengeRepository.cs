using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class ChallengeRepository(ApplicationDbContext dbContext)
    : BaseAsyncRepository<Challenge>(dbContext), IChallengeRepository
{
    private const string RecoverySourceType = "RECOVERY_MISSION";
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
            .Where(c => c.GameId == gameId && c.SourceType != RecoverySourceType)
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

    public async Task<int> CountActiveModuleChallengesAsync(int classroomId, int moduleId)
        => await DbContext.Challenges.AsNoTracking()
            .Where(c => c.ClassroomId == classroomId
                        && c.ModuleId == moduleId
                        && (c.SourceType == null || c.SourceType != RecoverySourceType)
                        && c.LifecycleState != ChallengeLifecycleState.Archived
                        && c.Status != "archived")
            .CountAsync();

    public async Task<Challenge> CreateChallengeAsync(Challenge challenge)
    {
        DbContext.Challenges.Add(challenge);
        await DbContext.SaveChangesAsync();
        return challenge;
    }

    public async Task UpdateChallengeAsync(Challenge challenge)
    {
        DbContext.Challenges.Update(challenge);

        try
        {
            await DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsMissingLifecycleColumn(ex))
        {
            // Backward compatibility:
            // Older databases may not have lifecycle columns yet.
            // Retry while skipping lifecycle fields so legacy behavior still works.
            var entry = DbContext.Entry(challenge);
            if (entry.State == EntityState.Detached)
            {
                DbContext.Challenges.Attach(challenge);
                entry = DbContext.Entry(challenge);
            }

            entry.State = EntityState.Modified;
            entry.Property(c => c.LifecycleState).IsModified = false;
            entry.Property(c => c.IsPinned).IsModified = false;
            entry.Property(c => c.RecommendedScore).IsModified = false;
            entry.Property(c => c.LastActivityAt).IsModified = false;

            await DbContext.SaveChangesAsync();
        }
    }

    private static bool IsMissingLifecycleColumn(DbUpdateException ex)
    {
        if (ex.InnerException is not PostgresException pgEx || pgEx.SqlState != "42703")
        {
            return false;
        }

        var message = pgEx.MessageText ?? string.Empty;
        return message.Contains("is_pinned", StringComparison.OrdinalIgnoreCase)
               || message.Contains("lifecycle_state", StringComparison.OrdinalIgnoreCase)
               || message.Contains("recommended_score", StringComparison.OrdinalIgnoreCase)
               || message.Contains("last_activity_at", StringComparison.OrdinalIgnoreCase);
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
        // Atomic upsert — eliminates TOCTOU race condition when two concurrent
        // submissions both finish at the same time and both read existing == null.
        // PostgreSQL executes the INSERT and the ON CONFLICT UPDATE as a single
        // atomic operation so no row can be double-inserted.
        await DbContext.Database.ExecuteSqlAsync($"""
            INSERT INTO "ChallengeProgresses"
                ("UserId", "ChallengeId", "ClassroomId",
                 "AttemptCount", "HasCompleted",
                 "BestScore", "BestStars", "BestAccuracy", "BestDurationSeconds",
                 "TotalXpEarned", "LastAttemptAt", "FirstCompletedAt",
                 "CreatedTime", "ModifiedDate")
            VALUES
                ({incoming.UserId}, {incoming.ChallengeId}, {incoming.ClassroomId},
                 {incoming.AttemptCount}, {incoming.HasCompleted},
                 {incoming.BestScore}, {incoming.BestStars}, {incoming.BestAccuracy}, {incoming.BestDurationSeconds},
                 {incoming.TotalXpEarned}, {incoming.LastAttemptAt}, {incoming.FirstCompletedAt},
                 NOW(), NOW())
            ON CONFLICT ("UserId", "ChallengeId", "ClassroomId") DO UPDATE SET
                "AttemptCount"        = EXCLUDED."AttemptCount",
                "HasCompleted"        = EXCLUDED."HasCompleted",
                "BestScore"           = EXCLUDED."BestScore",
                "BestStars"           = EXCLUDED."BestStars",
                "BestAccuracy"        = EXCLUDED."BestAccuracy",
                "BestDurationSeconds" = EXCLUDED."BestDurationSeconds",
                "TotalXpEarned"       = EXCLUDED."TotalXpEarned",
                "LastAttemptAt"       = EXCLUDED."LastAttemptAt",
                "FirstCompletedAt"    = COALESCE("ChallengeProgresses"."FirstCompletedAt", EXCLUDED."FirstCompletedAt"),
                "ModifiedDate"        = NOW()
            """);
    }

    public async Task<List<ChallengeProgress>> GetChallengeLeaderboardAsync(int challengeId, int classroomId)
        => await DbContext.ChallengeProgresses.AsNoTracking()
            .Include(cp => cp.User)
            .Where(cp => cp.ChallengeId == challengeId && cp.ClassroomId == classroomId)
            .OrderByDescending(cp => cp.BestScore)
            .ThenBy(cp => cp.BestDurationSeconds)
            .ThenBy(cp => cp.AttemptCount)
            .ToListAsync();

    public async Task<IReadOnlyDictionary<int, ChallengeLeaderboardSnapshot>> GetChallengeLeaderboardSnapshotsAsync(
        int classroomId,
        IReadOnlyCollection<int> challengeIds)
    {
        if (challengeIds.Count == 0)
        {
            return new Dictionary<int, ChallengeLeaderboardSnapshot>();
        }

        var snapshots = await DbContext.ChallengeProgresses.AsNoTracking()
            .Where(cp => cp.ClassroomId == classroomId && challengeIds.Contains(cp.ChallengeId))
            .GroupBy(cp => cp.ChallengeId)
            .Select(group => new ChallengeLeaderboardSnapshot(
                group.Key,
                group.Count(cp => cp.HasCompleted),
                group.Max(cp => cp.LastAttemptAt)))
            .ToListAsync();

        return snapshots.ToDictionary(snapshot => snapshot.ChallengeId);
    }

    public async Task<ChallengeProgress?> GetStudentChallengeProgressAsync(int userId, int challengeId, int classroomId)
        => await DbContext.ChallengeProgresses.AsNoTracking()
            .FirstOrDefaultAsync(cp =>
                cp.UserId == userId &&
                cp.ChallengeId == challengeId &&
                cp.ClassroomId == classroomId);

    public async Task<List<ChallengeProgress>> GetStudentProgressForClassroomAsync(int userId, int classroomId)
        => await DbContext.ChallengeProgresses.AsNoTracking()
            .Where(cp => cp.UserId == userId && cp.ClassroomId == classroomId)
            .ToListAsync();

    public async Task<bool> IsStudentModuleCompletedAsync(int userId, int classroomId, int moduleId)
    {
        var challengeIds = await DbContext.Challenges.AsNoTracking()
            .Where(challenge =>
                challenge.ClassroomId == classroomId &&
                challenge.ModuleId == moduleId &&
                (challenge.SourceType == null || challenge.SourceType != RecoverySourceType))
            .Select(challenge => challenge.Id)
            .ToListAsync();

        if (challengeIds.Count == 0)
        {
            return false;
        }

        var completedCount = await DbContext.ChallengeProgresses.AsNoTracking()
            .Where(progress =>
                progress.UserId == userId &&
                progress.ClassroomId == classroomId &&
                progress.HasCompleted &&
                challengeIds.Contains(progress.ChallengeId))
            .Select(progress => progress.ChallengeId)
            .Distinct()
            .CountAsync();

        return completedCount >= challengeIds.Count;
    }

    public async Task CompleteHardcoreChallengeRewardsAsync(int userId, int challengeId, CancellationToken cancellationToken)
    {
        var hardcoreDraft = await DbContext.HardcoreChallengeDrafts
            .FirstOrDefaultAsync(d => d.LinkedChallengeId == challengeId && d.StudentId == userId && d.Status == "ACCEPTED", cancellationToken);

        if (hardcoreDraft != null)
        {
            hardcoreDraft.Status = "COMPLETED";
            hardcoreDraft.CompletedAt = DateTime.UtcNow;

            // Grant configurable rewards
            var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user != null)
            {
                user.Experience += hardcoreDraft.RewardXp;
                user.Diamonds += hardcoreDraft.RewardDiamonds;
            }

            var userProgress = await DbContext.UserProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
            if (userProgress != null)
            {
                userProgress.TotalXP += hardcoreDraft.RewardXp;
                userProgress.ModifiedDate = DateTime.UtcNow;

                var nextLevel = await DbContext.Levels.AsNoTracking()
                    .Where(l => l.LevelNumber > userProgress.CurrentLevel && l.RequiredXP <= userProgress.TotalXP)
                    .OrderByDescending(l => l.LevelNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                if (nextLevel is not null)
                {
                    userProgress.CurrentLevel = nextLevel.LevelNumber;
                    if (user is not null)
                    {
                        user.Level = nextLevel.LevelNumber;
                    }
                }
            }

            // Unlock limited edition mascot
            if (hardcoreDraft.MascotEligibility && !string.IsNullOrWhiteSpace(hardcoreDraft.MascotName))
            {
                var mascotItem = await DbContext.ShopItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Name == hardcoreDraft.MascotName && item.Category == "avatar", cancellationToken);

                if (mascotItem != null)
                {
                    var alreadyOwned = await DbContext.UserInventoryItems
                        .AnyAsync(ii => ii.UserId == userId && ii.ShopItemId == mascotItem.Id, cancellationToken);
                    if (!alreadyOwned)
                    {
                        var invItem = new CleanArc.Domain.Entities.Shop.UserInventoryItem
                        {
                            UserId = userId,
                            ShopItemId = mascotItem.Id,
                            AcquiredAt = DateTime.UtcNow
                        };
                        DbContext.UserInventoryItems.Add(invItem);
                    }
                }
            }

            // Grant exclusive badge progression
            if (!string.IsNullOrWhiteSpace(hardcoreDraft.BadgeCode))
            {
                var badge = await DbContext.Badges
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Code == hardcoreDraft.BadgeCode, cancellationToken);

                if (badge != null)
                {
                    var progress = await DbContext.UserBadgeProgresses
                        .FirstOrDefaultAsync(bp => bp.UserId == userId && bp.BadgeId == badge.Id, cancellationToken);

                    if (progress == null)
                    {
                        progress = new CleanArc.Domain.Entities.Achievement.UserBadgeProgress
                        {
                            UserId = userId,
                            BadgeId = badge.Id,
                            ProgressValue = 1,
                            LastEvaluatedAt = DateTime.UtcNow
                        };
                        DbContext.UserBadgeProgresses.Add(progress);
                    }
                    else
                    {
                        progress.ProgressValue += 1;
                        progress.LastEvaluatedAt = DateTime.UtcNow;
                    }
                }
            }

            await DbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
