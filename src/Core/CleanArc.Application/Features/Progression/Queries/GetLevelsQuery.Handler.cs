using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Progression.Queries;

internal class GetLevelsQueryHandler : IRequestHandler<GetLevelsQuery, OperationResult<List<LevelDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetLevelsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<LevelDto>>> Handle(GetLevelsQuery request, CancellationToken cancellationToken)
  {
    var levels = await _unitOfWork.ProgressionRepository.GetAllLevelsAsync();
    var result = levels.Select(l => new LevelDto(l.Id, l.LevelNumber, l.Name, l.RequiredXP, l.UnlocksGameType)).ToList();
    return OperationResult<List<LevelDto>>.SuccessResult(result);
  }
}
