using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Queries;

internal sealed class GetStudentAchievementsQueryHandler(
  IAchievementTrackingService achievementTrackingService)
  : IRequestHandler<GetStudentAchievementsQuery, OperationResult<IReadOnlyList<StudentAchievementDto>>>
{
  public async ValueTask<OperationResult<IReadOnlyList<StudentAchievementDto>>> Handle(
    GetStudentAchievementsQuery request,
    CancellationToken cancellationToken)
  {
    var achievements = await achievementTrackingService.GetStudentAchievementsAsync(
      request.UserId,
      cancellationToken);

    return OperationResult<IReadOnlyList<StudentAchievementDto>>.SuccessResult(achievements);
  }
}
