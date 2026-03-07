using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Streaks.Commands;

public record CheckInCommand(int UserId) : IRequest<OperationResult<CheckInResult>>;

public record CheckInResult(int CurrentStreak, int BestStreak, bool IsNewDay);
