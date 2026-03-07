using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Shop;
using Mediator;

namespace CleanArc.Application.Features.Missions.Commands;

internal class ClaimMissionRewardCommandHandler : IRequestHandler<ClaimMissionRewardCommand, OperationResult<bool>>
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IAppUserManager _userManager;

  public ClaimMissionRewardCommandHandler(IUnitOfWork unitOfWork, IAppUserManager userManager)
  {
    _unitOfWork = unitOfWork;
    _userManager = userManager;
  }

  public async ValueTask<OperationResult<bool>> Handle(ClaimMissionRewardCommand request, CancellationToken cancellationToken)
  {
    var userMission = await _unitOfWork.MissionRepository.GetUserMissionAsync(request.UserId, request.MissionId);
    if (userMission == null)
      return OperationResult<bool>.NotFoundResult("Mission not found for user");

    if (!userMission.IsCompleted)
      return OperationResult<bool>.FailureResult("Mission is not completed yet");

    if (userMission.ClaimedAt != null)
      return OperationResult<bool>.FailureResult("Reward already claimed");

    var mission = userMission.Mission;

    // Grant reward based on type
    switch (mission.RewardType.ToLower())
    {
      case "diamonds":
        var user = await _userManager.GetUserByIdAsync(request.UserId);
        if (user != null)
        {
          user.Diamonds += mission.RewardAmount;
          await _userManager.UpdateUserAsync(user);

          await _unitOfWork.ShopRepository.AddDiamondTransactionAsync(new DiamondTransaction
          {
            UserId = request.UserId,
            Amount = mission.RewardAmount,
            Reason = $"Mission reward: {mission.Title}",
            ReferenceId = mission.Id.ToString()
          });
        }
        break;

      case "xp":
        await _unitOfWork.ProgressionRepository.AddXpAsync(request.UserId, mission.RewardAmount);
        break;
    }

    await _unitOfWork.MissionRepository.ClaimMissionRewardAsync(request.UserId, request.MissionId);
    return OperationResult<bool>.SuccessResult(true);
  }
}
