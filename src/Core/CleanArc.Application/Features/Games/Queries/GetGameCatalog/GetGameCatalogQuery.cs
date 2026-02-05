using CleanArc.Application.Features.Games.Models;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Queries.GetGameCatalog;

public record GetGameCatalogQuery : IRequest<OperationResult<IReadOnlyList<GameCatalogItemDto>>>;
