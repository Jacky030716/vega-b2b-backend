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
    return OperationResult<GetStreakResult>.SuccessResult(
        new GetStreakResult(streak.CurrentStreak, streak.BestStreak, streak.LastCheckInDate));
  }
}
