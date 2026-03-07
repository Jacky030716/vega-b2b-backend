using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Missions.Commands;

public record ClaimMissionRewardCommand(int UserId, int MissionId) : IRequest<OperationResult<bool>>;
