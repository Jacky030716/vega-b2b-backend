using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Progression.Queries;

internal class GetUserProgressQueryHandler : IRequestHandler<GetUserProgressQuery, OperationResult<UserProgressDto>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetUserProgressQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<UserProgressDto>> Handle(GetUserProgressQuery request, CancellationToken cancellationToken)
  {
    var progress = await _unitOfWork.ProgressionRepository.GetOrCreateUserProgressAsync(request.UserId);
    var currentLevel = await _unitOfWork.ProgressionRepository.GetLevelByNumberAsync(progress.CurrentLevel);
    var nextLevel = await _unitOfWork.ProgressionRepository.GetLevelByNumberAsync(progress.CurrentLevel + 1);

    var result = new UserProgressDto(
        progress.TotalXP,
        progress.CurrentLevel,
        currentLevel?.Name ?? $"Level {progress.CurrentLevel}",
        progress.TotalQuizzesTaken,
        progress.TotalCorrectAnswers,
        progress.TotalTimePlayed,
        nextLevel?.RequiredXP);

    return OperationResult<UserProgressDto>.SuccessResult(result);
  }
}
