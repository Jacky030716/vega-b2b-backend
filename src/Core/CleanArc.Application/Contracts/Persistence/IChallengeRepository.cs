using CleanArc.Domain.Entities.Quiz;

namespace CleanArc.Application.Contracts.Persistence;

public interface IChallengeRepository
{
  // Games
  Task<List<Game>> GetAllGamesAsync();
  Task<Game?> GetGameByKeyAsync(string gameKey);

  // Challenges
  Task<List<Challenge>> GetChallengesForGameAsync(int gameId);
  Task<int> GetNextOrderIndexForGameAsync(int gameId);
  Task<Challenge?> GetChallengeByIdAsync(int challengeId);
  Task<int> CountChallengesCreatedByTeacherAsync(int teacherId);
  Task<Challenge> CreateChallengeAsync(Challenge challenge);
  Task UpdateChallengeAsync(Challenge challenge);

  // Attempts
  Task<Attempt> CreateAttemptAsync(Attempt attempt);
  Task<Attempt?> GetAttemptByIdAsync(int attemptId);
  Task UpdateAttemptAsync(Attempt attempt);

  /// <summary>
  /// Returns the best (highest score) attempt per challenge for a user.
  /// Used to compute adventure map node star ratings and unlock status.
  /// </summary>
  Task<List<Attempt>> GetUserBestAttemptsForGameAsync(int userId, int gameId);

  /// <summary>
  /// Returns a prior completed attempt for a specific challenge by this user,
  /// or null if this is their first completion.
  /// </summary>
  Task<Attempt?> GetPriorCompletedAttemptForChallengeAsync(int userId, int challengeId, int excludeAttemptId);

  // Challenge Progress (leaderboard aggregates)

  /// <summary>
  /// Inserts or updates the aggregated progress row for a (student, challenge, classroom) triple.
  /// Called on every successful attempt completion.
  /// </summary>
  Task UpsertChallengeProgressAsync(ChallengeProgress progress);

  /// <summary>
  /// Returns all student progress rows for a challenge in a classroom, ranked by BestScore desc.
  /// </summary>
  Task<List<ChallengeProgress>> GetChallengeLeaderboardAsync(int challengeId, int classroomId);

  /// <summary>
  /// Returns a single student's progress row for a (challenge, classroom) pair, or null if none.
  /// </summary>
  Task<ChallengeProgress?> GetStudentChallengeProgressAsync(int userId, int challengeId, int classroomId);
}

