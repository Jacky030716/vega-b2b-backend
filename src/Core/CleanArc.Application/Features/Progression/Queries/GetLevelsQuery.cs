using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Progression.Queries;

public record GetLevelsQuery() : IRequest<OperationResult<List<LevelDto>>>;

public record LevelDto(int Id, int LevelNumber, string Name, int RequiredXP, string? UnlocksGameType);
