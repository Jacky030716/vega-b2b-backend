using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Commands;

internal sealed class SyncStudentAchievementsCommandHandler(
  IAchievementTrackingService achievementTrackingService)
  : IRequestHandler<SyncStudentAchievementsCommand, OperationResult<SyncStudentAchievementsResult>>
{
  public async ValueTask<OperationResult<SyncStudentAchievementsResult>> Handle(
    SyncStudentAchievementsCommand request,
    CancellationToken cancellationToken)
  {
    var unlocked = await achievementTrackingService.SyncStudentAchievementsAsync(
      request.UserId,
      cancellationToken);

    return OperationResult<SyncStudentAchievementsResult>.SuccessResult(
      new SyncStudentAchievementsResult(unlocked));
  }
}
