using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Queries;

// ─── DTOs ────────────────────────────────────────────────────────────────────

/// <summary>
/// Represents one node on the student's adventure map.
/// Stars and isUnlocked are computed server-side per-user.
/// </summary>
public record ChallengeNodeDto(
    int Id,
    int GameId,
    string GameKey,
    string Title,
    string Description,
    int DifficultyLevel,
    int OrderIndex,
    int MaxStars,
    /// <summary>Best stars the student has earned on this challenge (0 if never attempted).</summary>
    int BestStars,
    bool IsCompleted,
    /// <summary>Unlocked when the previous node is completed OR it's the first node.</summary>
    bool IsUnlocked,
    string ContentData
);

// ─── Query ───────────────────────────────────────────────────────────────────

/// <summary>
/// Returns all challenge nodes for a game, enriched with the student's personal progress.
/// </summary>
public record GetChallengesForGameQuery(string GameKey, int UserId)
    : IRequest<OperationResult<List<ChallengeNodeDto>>>;
