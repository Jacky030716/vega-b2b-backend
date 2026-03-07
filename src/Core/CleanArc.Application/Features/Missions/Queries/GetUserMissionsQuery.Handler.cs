using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Missions.Queries;

internal class GetUserMissionsQueryHandler : IRequestHandler<GetUserMissionsQuery, OperationResult<List<UserMissionDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetUserMissionsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<UserMissionDto>>> Handle(GetUserMissionsQuery request, CancellationToken cancellationToken)
  {
    var userMissions = await _unitOfWork.MissionRepository.GetUserMissionsAsync(request.UserId);
    var result = userMissions.Select(um => new UserMissionDto(
        um.MissionId, um.Mission.Title, um.Mission.Description, um.Mission.MissionType,
        um.Mission.RewardType, um.Mission.RewardAmount, um.Progress,
        um.IsCompleted, um.CompletedAt, um.ClaimedAt)).ToList();
    return OperationResult<List<UserMissionDto>>.SuccessResult(result);
  }
}
