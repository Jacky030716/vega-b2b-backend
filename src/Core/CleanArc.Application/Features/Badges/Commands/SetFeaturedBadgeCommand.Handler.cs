using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Badges.Commands;

internal class SetFeaturedBadgeCommandHandler : IRequestHandler<SetFeaturedBadgeCommand, OperationResult<bool>>
{
  private readonly IUnitOfWork _unitOfWork;

  public SetFeaturedBadgeCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<bool>> Handle(SetFeaturedBadgeCommand request, CancellationToken cancellationToken)
  {
    if (request.SlotIndex < 0 || request.SlotIndex > 2)
      return OperationResult<bool>.FailureResult("Slot index must be between 0 and 2");

    await _unitOfWork.BadgeRepository.SetFeaturedBadgeAsync(request.UserId, request.BadgeId, request.SlotIndex);
    return OperationResult<bool>.SuccessResult(true);
  }
}
