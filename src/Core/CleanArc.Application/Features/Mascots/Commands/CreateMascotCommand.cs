using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Mascots.Commands;

public record CreateMascotCommand(string Name, string ImageUrl, string Description, bool IsDefault, string? UnlockCondition)
    : IRequest<OperationResult<int>>;
