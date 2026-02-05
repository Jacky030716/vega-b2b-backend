using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Games.Models;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Queries.GetGameConfig;

public class GetGameConfigQueryHandler(IQuizContentRepository quizRepository)
    : IRequestHandler<GetGameConfigQuery, OperationResult<GameConfigDto>>
{
  public async ValueTask<OperationResult<GameConfigDto>> Handle(GetGameConfigQuery request, CancellationToken cancellationToken)
  {
    var config = await quizRepository.GetGameConfigAsync(request.GameType);
    if (config is null)
    {
      return OperationResult<GameConfigDto>.NotFoundResult("Game type not found");
    }

    return OperationResult<GameConfigDto>.SuccessResult(config);
  }
}
