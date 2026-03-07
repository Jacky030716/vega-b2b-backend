using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Progression.Commands;

internal class AddXpCommandHandler : IRequestHandler<AddXpCommand, OperationResult<AddXpResult>>
{
  private readonly IUnitOfWork _unitOfWork;

  public AddXpCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<AddXpResult>> Handle(AddXpCommand request, CancellationToken cancellationToken)
  {
    var progress = await _unitOfWork.ProgressionRepository.GetOrCreateUserProgressAsync(request.UserId);
    var previousLevel = progress.CurrentLevel;

    await _unitOfWork.ProgressionRepository.AddXpAsync(request.UserId, request.XpAmount);

    // Re-fetch to get updated values
    progress = await _unitOfWork.ProgressionRepository.GetUserProgressAsync(request.UserId);
    var leveledUp = progress.CurrentLevel > previousLevel;

    return OperationResult<AddXpResult>.SuccessResult(
        new AddXpResult(progress.TotalXP, progress.CurrentLevel, leveledUp));
  }
}
