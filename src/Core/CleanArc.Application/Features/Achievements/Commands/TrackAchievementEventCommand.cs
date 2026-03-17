using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Commands;

public record TrackAchievementEventCommand(
    int UserId,
    string EventType,
    string EventId,
    string PropertiesJson
) : IRequest<OperationResult<TrackAchievementEventResult>>;

public record TrackAchievementEventResult(
    IReadOnlyList<int> UnlockedBadgeIds
);
