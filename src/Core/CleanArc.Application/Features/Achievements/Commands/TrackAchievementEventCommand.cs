using System.Text.Json;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Commands;

public record TrackAchievementEventCommand(
    int UserId,
    string EventType,
    string EventId,
    JsonElement Properties,
    string? Timestamp = null
) : IRequest<OperationResult<TrackAchievementEventResult>>;

public record TrackAchievementEventResult(
    IReadOnlyList<int> UnlockedBadgeIds
);
