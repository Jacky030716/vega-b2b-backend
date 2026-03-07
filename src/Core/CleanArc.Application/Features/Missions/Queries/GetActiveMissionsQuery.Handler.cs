using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Missions.Queries;

internal class GetActiveMissionsQueryHandler : IRequestHandler<GetActiveMissionsQuery, OperationResult<List<MissionDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetActiveMissionsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<MissionDto>>> Handle(GetActiveMissionsQuery request, CancellationToken cancellationToken)
  {
    var missions = await _unitOfWork.MissionRepository.GetActiveMissionsAsync();
    var result = missions.Select(m => new MissionDto(
        m.Id, m.Title, m.Description, m.MissionType, m.RewardType, m.RewardAmount,
        m.RewardBadgeId, m.StartDate, m.EndDate)).ToList();
    return OperationResult<List<MissionDto>>.SuccessResult(result);
  }
}
