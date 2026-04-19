using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Streaks.Commands;

/// <summary>
/// Claims the weekly mystery reward after a qualifying 7-day streak milestone check-in.
/// </summary>
/// <param name="UserId">Authenticated user id.</param>
public record ClaimWeeklyMysteryRewardCommand(int UserId) : IRequest<OperationResult<ClaimWeeklyMysteryRewardResult>>;

/// <summary>
/// Response payload for weekly mystery reward claim attempts.
/// </summary>
public record ClaimWeeklyMysteryRewardResult(
  bool IsEligible,
  bool AlreadyClaimed,
  int DiamondsEarned,
  bool AwardedMascot,
  MysteryMascotRewardDto? Mascot,
  string Message);

/// <summary>
/// Mascot reward details when the mystery claim grants an avatar item.
/// </summary>
public record MysteryMascotRewardDto(
  int ShopItemId,
  string Name,
  string Rarity,
  string ImageUrl);
