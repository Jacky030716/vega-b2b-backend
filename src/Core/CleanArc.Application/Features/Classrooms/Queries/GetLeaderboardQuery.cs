using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

public record GetLeaderboardQuery(string QuizId, int? ClassroomId = null) : IRequest<OperationResult<List<LeaderboardDto>>>;

public record LeaderboardDto(int UserId, string UserName, int Score, int TotalPoints, double Percentage, int TimeSpent, DateTime CompletedAt);
