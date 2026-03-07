using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Missions.Commands;

public record CreateMissionCommand(string Title, string Description, string MissionType, string RewardType, int RewardAmount, int? RewardBadgeId, string? RequiredAction, DateTime StartDate, DateTime EndDate)
    : IRequest<OperationResult<int>>;
