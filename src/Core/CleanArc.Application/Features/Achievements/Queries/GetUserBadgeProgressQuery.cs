using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Queries;

public record GetUserBadgeProgressQuery(int UserId) : IRequest<OperationResult<List<UserBadgeProgressDto>>>;

public record UserBadgeProgressDto(
    int BadgeId,
    decimal CurrentProgress,
    decimal TargetProgress,
    bool IsUnlocked,
    string? AggregationType,
    string? AggregationSourceField,
    string? ProgressLabel
);
