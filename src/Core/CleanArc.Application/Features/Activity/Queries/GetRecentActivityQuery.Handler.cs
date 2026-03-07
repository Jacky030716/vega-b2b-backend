using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Activity.Queries;

internal class GetRecentActivityQueryHandler : IRequestHandler<GetRecentActivityQuery, OperationResult<List<ActivityDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetRecentActivityQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<ActivityDto>>> Handle(GetRecentActivityQuery request, CancellationToken cancellationToken)
  {
    var activities = await _unitOfWork.ActivityLogRepository.GetRecentActivityAsync(request.UserId, request.Count);
    var result = activities.Select(a => new ActivityDto(
        a.Id, a.Type, a.Title, a.Description, a.PointsEarned, a.ReferenceId, a.CreatedTime)).ToList();
    return OperationResult<List<ActivityDto>>.SuccessResult(result);
  }
}
