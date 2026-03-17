using System.Text.Json;
using CleanArc.Application.Contracts.Achievements;
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

internal class CompleteAttemptCommandHandler(
  IUnitOfWork unitOfWork,
  IAchievementTrackingService achievementTrackingService)
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

    var challenge = await unitOfWork.ChallengeRepository.GetChallengeByIdAsync(attempt.ChallengeId);

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

    // Dynamic achievement tracking for completion/performance achievements.
    var completedAt = attempt.CompletedAt;
    var (durationSeconds, accuracy) = ParseAttemptMetrics(request.AttemptData);
    var achievementPayload = JsonSerializer.Serialize(new
    {
      attemptId = attempt.Id,
      challengeId = attempt.ChallengeId,
      gameId = challenge?.GameId,
      gameKey = challenge?.Game?.Key,
      score = attempt.Score,
      starsEarned = clampedStars,
      isFirstCompletion,
      durationSeconds,
      accuracy,
      completedHourUtc = completedAt.Hour,
    });

    await achievementTrackingService.TrackEventAsync(
        request.UserId,
        "attempt_completed",
        $"attempt-completed:{attempt.Id}",
        achievementPayload,
        cancellationToken);

    if (coins > 0)
    {
      await achievementTrackingService.TrackEventAsync(
          request.UserId,
          "diamond_earned",
          $"diamond-earned:attempt:{attempt.Id}",
          JsonSerializer.Serialize(new
          {
            amount = coins,
            source = "attempt_completed",
            attemptId = attempt.Id,
            challengeId = attempt.ChallengeId,
            gameId = challenge?.GameId,
            gameKey = challenge?.Game?.Key,
          }),
          cancellationToken);
    }

    return OperationResult<CompleteAttemptDto>.SuccessResult(
        new CompleteAttemptDto(attempt.Id, attempt.Score, clampedStars, xp, coins, isFirstCompletion));
  }

  private static (decimal? DurationSeconds, decimal? Accuracy) ParseAttemptMetrics(string attemptData)
  {
    if (string.IsNullOrWhiteSpace(attemptData))
      return (null, null);

    try
    {
      using var doc = JsonDocument.Parse(attemptData);
      if (doc.RootElement.ValueKind != JsonValueKind.Object)
        return (null, null);

      var root = doc.RootElement;
      var duration = TryReadDecimal(root, "durationSeconds")
          ?? TryReadDecimal(root, "timeTakenSeconds")
          ?? TryReadDecimal(root, "duration");

      var accuracy = TryReadDecimal(root, "accuracy")
          ?? TryReadDecimal(root, "accuracyRate")
          ?? TryReadDecimal(root, "correctRate");

      return (duration, accuracy);
    }
    catch
    {
      return (null, null);
    }
  }

  private static decimal? TryReadDecimal(JsonElement root, string propertyName)
  {
    if (!root.TryGetProperty(propertyName, out var value))
      return null;

    if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
      return number;

    if (value.ValueKind == JsonValueKind.String
        && decimal.TryParse(value.GetString(), out var parsed))
      return parsed;

    return null;
  }
}
