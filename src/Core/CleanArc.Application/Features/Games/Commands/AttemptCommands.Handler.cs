using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Quiz;
using Mediator;

namespace CleanArc.Application.Features.Games.Commands;

// ─── Create Attempt Handler ───────────────────────────────────────────────────

internal class CreateAttemptCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAttemptCommand, OperationResult<CreateAttemptDto>>
{
  public async ValueTask<OperationResult<CreateAttemptDto>> Handle(
      CreateAttemptCommand request, CancellationToken cancellationToken)
  {
    var challenge = await unitOfWork.ChallengeRepository.GetChallengeByIdAsync(request.ChallengeId);
    if (challenge is null)
      return OperationResult<CreateAttemptDto>.NotFoundResult($"Challenge {request.ChallengeId} not found");

    var attempt = await unitOfWork.ChallengeRepository.CreateAttemptAsync(new Attempt
    {
      UserId = request.UserId,
      ChallengeId = request.ChallengeId,
      IsCompleted = false,
      Score = 0,
      StarsEarned = 0,
      XPEarned = 0,
      CoinsEarned = 0,
    });

    return OperationResult<CreateAttemptDto>.SuccessResult(
        new CreateAttemptDto(attempt.Id, challenge.Id));
  }
}

// ─── Complete Attempt Handler ─────────────────────────────────────────────────

internal class CompleteAttemptCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteAttemptCommand, OperationResult<CompleteAttemptDto>>
{
  // XP formula: 50 per star, minimum 10 for completing (even 0 stars)
  private static int CalcXP(int stars) => 10 + stars * 50;
  // Coins formula: 20 per star
  private static int CalcCoins(int stars) => stars * 20;

  public async ValueTask<OperationResult<CompleteAttemptDto>> Handle(
      CompleteAttemptCommand request, CancellationToken cancellationToken)
  {
    var attempt = await unitOfWork.ChallengeRepository.GetAttemptByIdAsync(request.AttemptId);
    if (attempt is null)
      return OperationResult<CompleteAttemptDto>.NotFoundResult("Attempt not found");

    if (attempt.UserId != request.UserId)
      return OperationResult<CompleteAttemptDto>.FailureResult("Forbidden");

    int clampedStars = Math.Clamp(request.StarsEarned, 0, 3);

    // Determine if this is the player's first time completing this challenge
    var priorAttempt = await unitOfWork.ChallengeRepository
        .GetPriorCompletedAttemptForChallengeAsync(request.UserId, attempt.ChallengeId, request.AttemptId);
    bool isFirstCompletion = priorAttempt is null;

    // Only award XP and diamonds on first completion to avoid farming
    int xp = isFirstCompletion ? CalcXP(clampedStars) : 0;
    int coins = isFirstCompletion ? CalcCoins(clampedStars) : 0;

    attempt.Score = request.Score;
    attempt.StarsEarned = clampedStars;
    attempt.XPEarned = xp;
    attempt.CoinsEarned = coins;
    attempt.IsCompleted = true;
    attempt.AttemptData = request.AttemptData;
    attempt.CompletedAt = DateTime.UtcNow;

    await unitOfWork.ChallengeRepository.UpdateAttemptAsync(attempt);

    // Award XP and diamonds (best-effort; non-blocking)
    if (isFirstCompletion)
    {
      try
      {
        await unitOfWork.ProgressionRepository.AddXpAsync(request.UserId, xp);
        await unitOfWork.ProgressionRepository.AddDiamondsAsync(request.UserId, coins);
      }
      catch
      {
        // Non-fatal — attempt is still recorded
      }
    }

    return OperationResult<CompleteAttemptDto>.SuccessResult(
        new CompleteAttemptDto(attempt.Id, attempt.Score, clampedStars, xp, coins, isFirstCompletion));
  }
}
