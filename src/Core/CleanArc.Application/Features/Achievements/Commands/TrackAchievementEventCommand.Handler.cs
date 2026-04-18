using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Models.Common;
using Mediator;
using System.Text.Json;

namespace CleanArc.Application.Features.Achievements.Commands;

internal class TrackAchievementEventCommandHandler(
    IAchievementTrackingService achievementTrackingService)
    : IRequestHandler<TrackAchievementEventCommand, OperationResult<TrackAchievementEventResult>>
{
    public async ValueTask<OperationResult<TrackAchievementEventResult>> Handle(
        TrackAchievementEventCommand request,
        CancellationToken cancellationToken)
    {
        var propertiesJson = request.Properties.ValueKind == JsonValueKind.Undefined
            ? "{}"
            : request.Properties.GetRawText();

        var unlockedBadgeIds = await achievementTrackingService.TrackEventAsync(
            request.UserId,
            request.EventType,
            request.EventId,
            propertiesJson,
            cancellationToken);

        return OperationResult<TrackAchievementEventResult>.SuccessResult(
            new TrackAchievementEventResult(unlockedBadgeIds));
    }
}
