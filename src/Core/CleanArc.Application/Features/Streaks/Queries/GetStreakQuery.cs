using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Streaks.Queries;

public record GetStreakQuery(int UserId) : IRequest<OperationResult<GetStreakResult>>;

public record GetStreakResult(int TotalCheckIns, List<DateOnly> RecentCheckIns, DateOnly? LastCheckInDate);
