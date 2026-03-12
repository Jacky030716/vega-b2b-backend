using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Commands;

internal class SetFeaturedBadgeCommandHandler : IRequestHandler<SetFeaturedBadgeCommand, OperationResult<bool>>
{
  private readonly IUnitOfWork _unitOfWork;

  public SetFeaturedBadgeCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<bool>> Handle(SetFeaturedBadgeCommand request, CancellationToken cancellationToken)
  {
    try
    {
      await _unitOfWork.BadgeRepository.SetFeaturedBadgeAsync(
          request.UserId, request.BadgeId, request.SlotIndex);
      return OperationResult<bool>.SuccessResult(true);
    }
    catch (InvalidOperationException ex)
    {
      return OperationResult<bool>.FailureResult(ex.Message);
    }
  }
}
