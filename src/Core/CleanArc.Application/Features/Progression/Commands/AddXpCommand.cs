using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Progression.Commands;

public record AddXpCommand(int UserId, int XpAmount, string Reason) : IRequest<OperationResult<AddXpResult>>;

public record AddXpResult(int TotalXP, int CurrentLevel, bool LeveledUp);
