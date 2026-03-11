using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Streaks.Queries;

internal class GetStreakQueryHandler : IRequestHandler<GetStreakQuery, OperationResult<GetStreakResult>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetStreakQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<GetStreakResult>> Handle(GetStreakQuery request, CancellationToken cancellationToken)
  {
    var streak = await _unitOfWork.StreakRepository.GetOrCreateUserStreakAsync(request.UserId);
    
    // Fetch last 60 days of check-ins for the chronological array
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var fromDate = today.AddDays(-60);
    var recentCheckIns = await _unitOfWork.StreakRepository.GetCheckInsForRangeAsync(request.UserId, fromDate, today);
    var dates = recentCheckIns.Select(c => c.CheckInDate).ToList();

    return OperationResult<GetStreakResult>.SuccessResult(
        new GetStreakResult(streak.CurrentStreak, dates, streak.LastCheckInDate));
  }
}
