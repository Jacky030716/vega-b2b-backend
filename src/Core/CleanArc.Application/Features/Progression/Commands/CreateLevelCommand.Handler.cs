using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Progression;
using Mediator;

namespace CleanArc.Application.Features.Progression.Commands;

internal class CreateLevelCommandHandler : IRequestHandler<CreateLevelCommand, OperationResult<int>>
{
  private readonly IUnitOfWork _unitOfWork;

  public CreateLevelCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<int>> Handle(CreateLevelCommand request, CancellationToken cancellationToken)
  {
    var existing = await _unitOfWork.ProgressionRepository.GetLevelByNumberAsync(request.LevelNumber);
    if (existing != null)
      return OperationResult<int>.FailureResult($"Level {request.LevelNumber} already exists");

    var level = new Level
    {
      LevelNumber = request.LevelNumber,
      Name = request.Name,
      RequiredXP = request.RequiredXP,
      UnlocksGameType = request.UnlocksGameType
    };

    var created = await _unitOfWork.ProgressionRepository.CreateLevelAsync(level);
    return OperationResult<int>.SuccessResult(created.Id);
  }
}
