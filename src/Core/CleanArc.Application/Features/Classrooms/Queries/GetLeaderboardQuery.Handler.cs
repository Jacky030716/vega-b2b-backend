using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, OperationResult<List<LeaderboardDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetLeaderboardQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<LeaderboardDto>>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
  {
    var entries = await _unitOfWork.ClassroomRepository.GetLeaderboardAsync(request.QuizId, request.ClassroomId);

    var result = entries.Select(e => new LeaderboardDto(
        e.UserId, e.User?.UserName ?? "", e.Score, e.TotalPoints, e.Percentage, e.TimeSpent, e.CompletedAt)).ToList();

    return OperationResult<List<LeaderboardDto>>.SuccessResult(result);
  }
}
