using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Commands;

public sealed record SyncStudentAchievementsCommand(int UserId)
  : IRequest<OperationResult<SyncStudentAchievementsResult>>;

public sealed record SyncStudentAchievementsResult(IReadOnlyList<int> UnlockedAchievementIds);
