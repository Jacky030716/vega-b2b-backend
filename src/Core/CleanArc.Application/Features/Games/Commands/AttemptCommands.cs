using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Commands;

// ─── Create Attempt ───────────────────────────────────────────────────────────

public record CreateAttemptDto(int AttemptId, int ChallengeId);

public record CreateAttemptCommand(int UserId, int ChallengeId)
    : IRequest<OperationResult<CreateAttemptDto>>;

// ─── Complete Attempt ─────────────────────────────────────────────────────────

public record CompleteAttemptDto(
    int AttemptId,
    int Score,
    int StarsEarned,
    int XPEarned,
    int CoinsEarned,
    /// <summary>True when this is the player's very first completion of this challenge.</summary>
    bool IsFirstCompletion
);

public record CompleteAttemptCommand(
    int UserId,
    int AttemptId,
    int Score,
    /// <summary>0–3 stars earned. Calulated by client based on game rules.</summary>
    int StarsEarned,
    string AttemptData
) : IRequest<OperationResult<CompleteAttemptDto>>;
