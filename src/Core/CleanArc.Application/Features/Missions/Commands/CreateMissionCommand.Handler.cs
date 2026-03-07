using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Mission;
using Mediator;

namespace CleanArc.Application.Features.Missions.Commands;

internal class CreateMissionCommandHandler : IRequestHandler<CreateMissionCommand, OperationResult<int>>
{
  private readonly IUnitOfWork _unitOfWork;

  public CreateMissionCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<int>> Handle(CreateMissionCommand request, CancellationToken cancellationToken)
  {
    var mission = new SpecialMission
    {
      Title = request.Title,
      Description = request.Description,
      MissionType = request.MissionType,
      RewardType = request.RewardType,
      RewardAmount = request.RewardAmount,
      RewardBadgeId = request.RewardBadgeId,
      RequiredAction = request.RequiredAction,
      StartDate = request.StartDate,
      EndDate = request.EndDate,
      IsActive = true
    };

    var created = await _unitOfWork.MissionRepository.CreateMissionAsync(mission);
    return OperationResult<int>.SuccessResult(created.Id);
  }
}
