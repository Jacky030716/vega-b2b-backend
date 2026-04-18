using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Queries;

// ─── DTO ─────────────────────────────────────────────────────────────────────

/// <summary>
/// One row on the challenge leaderboard inside a classroom.
/// </summary>
public record ChallengeLeaderboardEntryDto(
    int Rank,
    int UserId,
    string StudentName,
    string? AvatarId,
    int AttemptCount,
    bool HasCompleted,
    int BestScore,
    int BestStars,
    /// <summary>Accuracy as a percentage (0–100), e.g. 85.5. Null if not yet recorded.</summary>
    decimal? BestAccuracy,
    /// <summary>Fastest completion time in seconds. Null if not yet recorded.</summary>
    decimal? BestDurationSeconds,
    DateTime? FirstCompletedAt,
    DateTime LastAttemptAt
);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns the ranked leaderboard for a specific challenge within a classroom.
/// </summary>
public record GetChallengeLeaderboardQuery(int ChallengeId, int ClassroomId)
    : IRequest<OperationResult<List<ChallengeLeaderboardEntryDto>>>;
