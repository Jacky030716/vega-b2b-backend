using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Queries;

public sealed record GetStudentAchievementsQuery(int UserId)
  : IRequest<OperationResult<IReadOnlyList<StudentAchievementDto>>>;
