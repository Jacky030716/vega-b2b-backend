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
  private const string RarityLegendary = "legendary";
  private const string RarityRare = "rare";
  private const string RarityNormal = "common";
  private const int DiamondMin = 500;
  private const int DiamondMax = 2000;

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
          false,
          false,
          0,
          false,
          null,
          "Complete today on a 7-day streak milestone to claim this reward."));
    }

    var alreadyClaimedToday = await _unitOfWork.StreakRepository.HasClaimedMysteryRewardForDateAsync(request.UserId, today);
    if (alreadyClaimedToday)
    {
      return OperationResult<ClaimWeeklyMysteryRewardResult>.SuccessResult(
        new ClaimWeeklyMysteryRewardResult(
          true,
          true,
          0,
          false,
          null,
          "Weekly mystery reward already claimed for today."));
    }

    var rewardTier = RollRewardTier();
    var userMascotInventory = await _unitOfWork.ShopRepository.GetUserInventoryAsync(request.UserId, MascotCategory);
    var ownedMascotIds = userMascotInventory.Select(i => i.ShopItemId).ToHashSet();

    int diamondsEarned;
    MysteryMascotRewardDto? mascotReward = null;

    if (rewardTier == MysteryRewardTier.Diamonds)
    {
      diamondsEarned = Random.Shared.Next(DiamondMin, DiamondMax + 1);
    }
    else
    {
      var mascotItem = await TryPickMascotForTierAsync(rewardTier, ownedMascotIds);
      if (mascotItem is null)
      {
        diamondsEarned = Random.Shared.Next(DiamondMin, DiamondMax + 1);
      }
      else
      {
        await _unitOfWork.ShopRepository.AddToInventoryAsync(new UserInventoryItem
        {
          UserId = request.UserId,
          ShopItemId = mascotItem.Id,
          AcquiredAt = DateTime.UtcNow
        });

        diamondsEarned = GetMascotBonusDiamonds(rewardTier);
        mascotReward = new MysteryMascotRewardDto(
          mascotItem.Id,
          mascotItem.Name,
          mascotItem.Rarity,
          mascotItem.ImageUrl);
      }
    }

    await _unitOfWork.ProgressionRepository.AddDiamondsAsync(request.UserId, diamondsEarned);

    await _unitOfWork.ShopRepository.AddDiamondTransactionAsync(new DiamondTransaction
    {
      UserId = request.UserId,
      Amount = diamondsEarned,
      Reason = mascotReward is null ? "Weekly mystery reward (diamonds)" : "Weekly mystery reward (mascot + diamonds)",
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
        rarity = mascotReward?.Rarity,
      }),
      cancellationToken);

    return OperationResult<ClaimWeeklyMysteryRewardResult>.SuccessResult(
      new ClaimWeeklyMysteryRewardResult(
        true,
        false,
        diamondsEarned,
        mascotReward is not null,
        mascotReward,
        mascotReward is null
          ? "Mystery reward granted: diamonds."
          : "Mystery reward granted: mascot and diamonds."));
  }

  private static bool IsEligibleForMysteryClaim(CleanArc.Domain.Entities.Streak.UserStreak userStreak, DateOnly today)
  {
    if (userStreak.CurrentStreak <= 0)
    {
      return false;
    }

    var isMilestone = userStreak.CurrentStreak % 7 == 0;
    var checkedInToday = userStreak.LastCheckInDate == today;
    return isMilestone && checkedInToday;
  }

  private static MysteryRewardTier RollRewardTier()
  {
    var roll = Random.Shared.Next(1, 101);

    if (roll <= 1)
    {
      return MysteryRewardTier.LegendaryMascot;
    }

    if (roll <= 6)
    {
      return MysteryRewardTier.RareMascot;
    }

    if (roll <= 16)
    {
      return MysteryRewardTier.NormalMascot;
    }

    return MysteryRewardTier.Diamonds;
  }

  private async Task<ShopItem?> TryPickMascotForTierAsync(MysteryRewardTier tier, HashSet<int> ownedMascotIds)
  {
    var rarityAliases = tier switch
    {
      MysteryRewardTier.LegendaryMascot => new[] { RarityLegendary },
      MysteryRewardTier.RareMascot => new[] { RarityRare },
      MysteryRewardTier.NormalMascot => new[] { RarityNormal, "normal" },
      _ => Array.Empty<string>()
    };

    if (rarityAliases.Length == 0)
    {
      return null;
    }

    var candidates = await _unitOfWork.ShopRepository.GetShopItemsByCategoryAndRaritiesAsync(MascotCategory, rarityAliases);
    if (candidates.Count == 0)
    {
      return null;
    }

    var unowned = candidates.Where(c => !ownedMascotIds.Contains(c.Id)).ToList();
    var pool = unowned.Count > 0 ? unowned : candidates;
    return pool[Random.Shared.Next(0, pool.Count)];
  }

  private static int GetMascotBonusDiamonds(MysteryRewardTier tier)
  {
    return tier switch
    {
      MysteryRewardTier.LegendaryMascot => 1000,
      MysteryRewardTier.RareMascot => 750,
      MysteryRewardTier.NormalMascot => 500,
      _ => Random.Shared.Next(DiamondMin, DiamondMax + 1)
    };
  }

  private enum MysteryRewardTier
  {
    LegendaryMascot,
    RareMascot,
    NormalMascot,
    Diamonds
  }
}
