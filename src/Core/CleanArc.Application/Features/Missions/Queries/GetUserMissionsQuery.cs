using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Missions.Queries;

public record GetUserMissionsQuery(int UserId) : IRequest<OperationResult<List<UserMissionDto>>>;

public record UserMissionDto(int MissionId, string Title, string Description, string MissionType, string RewardType, int RewardAmount, int Progress, bool IsCompleted, DateTime? CompletedAt, DateTime? ClaimedAt);
