using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Streak;
using Mediator;

namespace CleanArc.Application.Features.Streaks.Commands;

internal class CheckInCommandHandler : IRequestHandler<CheckInCommand, OperationResult<CheckInResult>>
{
  private readonly IUnitOfWork _unitOfWork;

  public CheckInCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
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
          new CheckInResult(streak.CurrentStreak, dates, false));
    }

    // Create check-in
    await _unitOfWork.StreakRepository.AddCheckInAsync(new DailyCheckIn
    {
      UserId = request.UserId,
      CheckInDate = today
    });

    // Update streak (Always increment total)
    var userStreak = await _unitOfWork.StreakRepository.GetOrCreateUserStreakAsync(request.UserId);
    userStreak.CurrentStreak++;
    userStreak.LastCheckInDate = today;
    await _unitOfWork.StreakRepository.UpdateStreakAsync(userStreak);

    // Re-fetch array including the newly added check-in today
    var updatedCheckIns = await _unitOfWork.StreakRepository.GetCheckInsForRangeAsync(request.UserId, fromDate, today);
    var updatedDates = updatedCheckIns.Select(c => c.CheckInDate).ToList();

    return OperationResult<CheckInResult>.SuccessResult(
        new CheckInResult(userStreak.CurrentStreak, updatedDates, true));
  }
}
