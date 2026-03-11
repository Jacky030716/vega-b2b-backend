using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Queries;

internal class GetGamesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetGamesQuery, OperationResult<List<GameDto>>>
{
  public async ValueTask<OperationResult<List<GameDto>>> Handle(
      GetGamesQuery request, CancellationToken cancellationToken)
  {
    var games = await unitOfWork.ChallengeRepository.GetAllGamesAsync();
    var dtos = games
        .Select(g => new GameDto(g.Id, g.Key, g.Name, g.Description, g.ImageUrl, g.Category, g.SkillsTaught))
        .ToList();

    return OperationResult<List<GameDto>>.SuccessResult(dtos);
  }
}
