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

    if (existing != null)
    {
      var streak = await _unitOfWork.StreakRepository.GetUserStreakAsync(request.UserId);
      return OperationResult<CheckInResult>.SuccessResult(
          new CheckInResult(streak.CurrentStreak, streak.BestStreak, false));
    }

    // Create check-in
    await _unitOfWork.StreakRepository.AddCheckInAsync(new DailyCheckIn
    {
      UserId = request.UserId,
      CheckInDate = today
    });

    // Update streak
    var userStreak = await _unitOfWork.StreakRepository.GetOrCreateUserStreakAsync(request.UserId);
    var yesterday = today.AddDays(-1);

    if (userStreak.LastCheckInDate == yesterday)
    {
      userStreak.CurrentStreak++;
    }
    else
    {
      userStreak.CurrentStreak = 1;
    }

    if (userStreak.CurrentStreak > userStreak.BestStreak)
      userStreak.BestStreak = userStreak.CurrentStreak;

    userStreak.LastCheckInDate = today;
    await _unitOfWork.StreakRepository.UpdateStreakAsync(userStreak);

    return OperationResult<CheckInResult>.SuccessResult(
        new CheckInResult(userStreak.CurrentStreak, userStreak.BestStreak, true));
  }
}
