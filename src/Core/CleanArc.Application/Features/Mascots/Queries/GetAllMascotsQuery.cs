using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Mascots.Queries;

public record GetAllMascotsQuery() : IRequest<OperationResult<List<MascotDto>>>;

public record MascotDto(int Id, string Name, string ImageUrl, string Description, bool IsDefault, string? UnlockCondition);
