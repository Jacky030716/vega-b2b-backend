using System.Text.Json;
using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Shop;
using Mediator;

namespace CleanArc.Application.Features.Streaks.Commands;

internal class ClaimWeeklyMysteryRewardCommandHandler : IRequestHandler<ClaimWeeklyMysteryRewardCommand, OperationResult<ClaimWeeklyMysteryRewardResult>>
{
  private const string MascotCategory = "avatar";
  private const string SpyFamilyTheme = "spy_family";
  private const int WeeklyDiamonds = 3000;
  private const int DuplicateDiamonds = 3000;

  private readonly IUnitOfWork _unitOfWork;
  private readonly IAchievementTrackingService _achievementTrackingService;

  public ClaimWeeklyMysteryRewardCommandHandler(
    IUnitOfWork unitOfWork,
    IAchievementTrackingService achievementTrackingService)
  {
    _unitOfWork = unitOfWork;
    _achievementTrackingService = achievementTrackingService;
  }

  public async ValueTask<OperationResult<ClaimWeeklyMysteryRewardResult>> Handle(
    ClaimWeeklyMysteryRewardCommand request,
    CancellationToken cancellationToken)
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var userStreak = await _unitOfWork.StreakRepository.GetOrCreateUserStreakAsync(request.UserId);

    if (!IsEligibleForMysteryClaim(userStreak, today))
    {
      return OperationResult<ClaimWeeklyMysteryRewardResult>.SuccessResult(
        new ClaimWeeklyMysteryRewardResult(
          false, false, 0, false, false, null,
          "Complete today on a 7-day streak milestone to claim this reward."));
    }

    var alreadyClaimedToday = await _unitOfWork.StreakRepository.HasClaimedMysteryRewardForDateAsync(request.UserId, today);
    if (alreadyClaimedToday)
    {
      return OperationResult<ClaimWeeklyMysteryRewardResult>.SuccessResult(
        new ClaimWeeklyMysteryRewardResult(
          true, true, 0, false, false, null,
          "Weekly mystery reward already claimed for today."));
    }

    // Load all spy_family mascots and the user's current avatar inventory
    var allSpyFamilyMascots = await _unitOfWork.ShopRepository.GetShopItemsByCategoryAndRaritiesAsync(
      MascotCategory, "legendary", "rare", "common", "normal");
    var spyFamilyPool = allSpyFamilyMascots
      .Where(m => string.Equals(m.Theme, SpyFamilyTheme, StringComparison.OrdinalIgnoreCase))
      .ToList();

    var userInventory = await _unitOfWork.ShopRepository.GetUserInventoryAsync(request.UserId, MascotCategory);
    var ownedIds = userInventory.Select(i => i.ShopItemId).ToHashSet();

    int diamondsEarned;
    MysteryMascotRewardDto? mascotReward = null;
    bool isDuplicate = false;

    if (spyFamilyPool.Count == 0)
    {
      // No spy_family mascots seeded yet — diamonds only fallback
      diamondsEarned = WeeklyDiamonds;
    }
    else
    {
      // Pick a random spy_family mascot
      var picked = spyFamilyPool[Random.Shared.Next(0, spyFamilyPool.Count)];

      if (ownedIds.Contains(picked.Id))
      {
        // Already owns it — convert to diamonds
        isDuplicate = true;
        diamondsEarned = DuplicateDiamonds;
      }
      else
      {
        // Award the mascot
        await _unitOfWork.ShopRepository.TryAddToInventoryAsync(new UserInventoryItem
        {
          UserId = request.UserId,
          ShopItemId = picked.Id,
          AcquiredAt = DateTime.UtcNow
        });

        diamondsEarned = WeeklyDiamonds;
        mascotReward = new MysteryMascotRewardDto(picked.Id, picked.Name, picked.Rarity, picked.ImageUrl);
      }
    }

    await _unitOfWork.ProgressionRepository.AddDiamondsAsync(request.UserId, diamondsEarned);

    await _unitOfWork.ShopRepository.AddDiamondTransactionAsync(new DiamondTransaction
    {
      UserId = request.UserId,
      Amount = diamondsEarned,
      Reason = isDuplicate
        ? "Weekly mystery reward (duplicate mascot → diamonds)"
        : mascotReward is null
          ? "Weekly mystery reward (diamonds)"
          : "Weekly mystery reward (mascot + diamonds)",
      ReferenceId = $"weekly-mystery:{today:yyyy-MM-dd}"
    });

    await _unitOfWork.StreakRepository.MarkMysteryRewardClaimedAsync(userStreak, today);

    await _achievementTrackingService.TrackEventAsync(
      request.UserId,
      "diamond_earned",
      $"diamond-earned:weekly-mystery:{request.UserId}:{today:yyyy-MM-dd}",
      JsonSerializer.Serialize(new
      {
        amount = diamondsEarned,
        source = "weekly_mystery_reward",
        awardedMascot = mascotReward is not null,
        isDuplicate,
        rarity = mascotReward?.Rarity,
      }),
      cancellationToken);

    return OperationResult<ClaimWeeklyMysteryRewardResult>.SuccessResult(
      new ClaimWeeklyMysteryRewardResult(
        true,
        false,
        diamondsEarned,
        mascotReward is not null,
        isDuplicate,
        mascotReward,
        isDuplicate
          ? "Duplicate mascot! Converted to diamonds."
          : mascotReward is null
            ? "Mystery reward granted: diamonds."
            : "Mystery reward granted: mascot and diamonds."));
  }

  private static bool IsEligibleForMysteryClaim(CleanArc.Domain.Entities.Streak.UserStreak userStreak, DateOnly today)
  {
    if (userStreak.CurrentStreak <= 0) return false;
    var isMilestone = userStreak.CurrentStreak % 7 == 0;
    var checkedInToday = userStreak.LastCheckInDate == today;
    return isMilestone && checkedInToday;
  }
}
