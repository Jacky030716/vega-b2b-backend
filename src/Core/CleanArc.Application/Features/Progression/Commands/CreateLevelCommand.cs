using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Progression.Commands;

public record CreateLevelCommand(int LevelNumber, string Name, int RequiredXP, string? UnlocksGameType)
    : IRequest<OperationResult<int>>;
