using CleanArc.Application.Features.Games.Models;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Queries.GetGameConfig;

public record GetGameConfigQuery(string GameType) : IRequest<OperationResult<GameConfigDto>>;
