using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Games.Models;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Queries.GetGameCatalog;

public class GetGameCatalogQueryHandler(IQuizContentRepository quizRepository)
    : IRequestHandler<GetGameCatalogQuery, OperationResult<IReadOnlyList<GameCatalogItemDto>>>
{
  public async ValueTask<OperationResult<IReadOnlyList<GameCatalogItemDto>>> Handle(GetGameCatalogQuery request, CancellationToken cancellationToken)
  {
    var items = await quizRepository.GetGameCatalogAsync();
    return OperationResult<IReadOnlyList<GameCatalogItemDto>>.SuccessResult(items);
  }
}
