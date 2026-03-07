using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Missions.Queries;

public record GetActiveMissionsQuery() : IRequest<OperationResult<List<MissionDto>>>;

public record MissionDto(int Id, string Title, string Description, string MissionType, string RewardType, int RewardAmount, int? RewardBadgeId, DateTime StartDate, DateTime EndDate);
