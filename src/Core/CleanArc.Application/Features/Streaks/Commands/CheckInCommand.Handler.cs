using System.Text.Json;
using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Streak;
using Mediator;

namespace CleanArc.Application.Features.Streaks.Commands;

internal class CheckInCommandHandler : IRequestHandler<CheckInCommand, OperationResult<CheckInResult>>
{
  private const int BaseDailyReward = 50;
  private const int DailyIncrement = 10;
  private const int WeeklyBonusMin = 200;
  private const int WeeklyBonusMax = 500;

  private readonly IUnitOfWork _unitOfWork;
  private readonly IAchievementTrackingService _achievementTrackingService;

  public CheckInCommandHandler(
      IUnitOfWork unitOfWork,
      IAchievementTrackingService achievementTrackingService)
  {
    _unitOfWork = unitOfWork;
    _achievementTrackingService = achievementTrackingService;
  }

  public async ValueTask<OperationResult<CheckInResult>> Handle(CheckInCommand request, CancellationToken cancellationToken)
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var existing = await _unitOfWork.StreakRepository.GetCheckInAsync(request.UserId, today);

    // Fetch updated 60-day array
    var fromDate = today.AddDays(-60);
    var recentCheckIns = await _unitOfWork.StreakRepository.GetCheckInsForRangeAsync(request.UserId, fromDate, today);
    var dates = recentCheckIns.Select(c => c.CheckInDate).ToList();

    if (existing != null)
    {
      var streak = await _unitOfWork.StreakRepository.GetUserStreakAsync(request.UserId);
      return OperationResult<CheckInResult>.SuccessResult(
          new CheckInResult(streak.CurrentStreak, dates, false, 0, false));
    }

    // Create check-in
    await _unitOfWork.StreakRepository.AddCheckInAsync(new DailyCheckIn
    {
      UserId = request.UserId,
      CheckInDate = today
    });

    // Update streak (Always increment total)
    var userStreak = await _unitOfWork.StreakRepository.GetOrCreateUserStreakAsync(request.UserId);
    var yesterday = today.AddDays(-1);
    userStreak.CurrentStreak = userStreak.LastCheckInDate == yesterday
      ? userStreak.CurrentStreak + 1
      : 1;
    userStreak.BestStreak = Math.Max(userStreak.BestStreak, userStreak.CurrentStreak);
    userStreak.LastCheckInDate = today;
    await _unitOfWork.StreakRepository.UpdateStreakAsync(userStreak);

    var (diamondsEarned, isWeeklyBonus) = CalculateCheckInReward(userStreak.CurrentStreak);
    await _unitOfWork.ProgressionRepository.AddDiamondsAsync(request.UserId, diamondsEarned);

    // Dynamic achievement tracking for streak-style badges.
    var checkInEventId = $"daily-check-in:{request.UserId}:{today:yyyy-MM-dd}";
    var checkInEventPayload = JsonSerializer.Serialize(new
    {
      checkInDate = today.ToString("yyyy-MM-dd"),
      currentStreak = userStreak.CurrentStreak,
      isWeeklyBonus,
      diamondsEarned,
    });

    await _achievementTrackingService.TrackEventAsync(
        request.UserId,
        "daily_check_in",
        checkInEventId,
        checkInEventPayload,
        cancellationToken);

    await _achievementTrackingService.TrackEventAsync(
        request.UserId,
        "diamond_earned",
        $"diamond-earned:check-in:{request.UserId}:{today:yyyy-MM-dd}",
        JsonSerializer.Serialize(new
        {
          amount = diamondsEarned,
          source = "daily_check_in",
          currentStreak = userStreak.CurrentStreak,
        }),
        cancellationToken);

    // Re-fetch array including the newly added check-in today
    var updatedCheckIns = await _unitOfWork.StreakRepository.GetCheckInsForRangeAsync(request.UserId, fromDate, today);
    var updatedDates = updatedCheckIns.Select(c => c.CheckInDate).ToList();

    return OperationResult<CheckInResult>.SuccessResult(
        new CheckInResult(
            userStreak.CurrentStreak,
            updatedDates,
            true,
            diamondsEarned,
            isWeeklyBonus));
  }

  private static (int DiamondsEarned, bool IsWeeklyBonus) CalculateCheckInReward(int totalCheckIns)
  {
    var dayInCycle = ((totalCheckIns - 1) % 7) + 1;
    var isWeeklyBonus = dayInCycle == 7;

    if (isWeeklyBonus)
    {
      return (Random.Shared.Next(WeeklyBonusMin, WeeklyBonusMax + 1), true);
    }

    var reward = BaseDailyReward + ((dayInCycle - 1) * DailyIncrement);
    return (reward, false);
  }
}
