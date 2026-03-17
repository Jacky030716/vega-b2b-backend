using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Commands;

internal class TrackAchievementEventCommandHandler(
    IAchievementTrackingService achievementTrackingService)
    : IRequestHandler<TrackAchievementEventCommand, OperationResult<TrackAchievementEventResult>>
{
  public async ValueTask<OperationResult<TrackAchievementEventResult>> Handle(
      TrackAchievementEventCommand request,
      CancellationToken cancellationToken)
  {
    var unlockedBadgeIds = await achievementTrackingService.TrackEventAsync(
        request.UserId,
        request.EventType,
        request.EventId,
        request.PropertiesJson,
        cancellationToken);

    return OperationResult<TrackAchievementEventResult>.SuccessResult(
        new TrackAchievementEventResult(unlockedBadgeIds));
  }
}
