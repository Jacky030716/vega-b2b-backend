using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Achievement;
using Mediator;

namespace CleanArc.Application.Features.Badges.Commands;

internal class CreateBadgeCommandHandler : IRequestHandler<CreateBadgeCommand, OperationResult<int>>
{
  private readonly IUnitOfWork _unitOfWork;

  public CreateBadgeCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<int>> Handle(CreateBadgeCommand request, CancellationToken cancellationToken)
  {
    var badge = new Badge
    {
      Name = request.Name,
      Description = request.Description,
      BadgeImageUrl = request.BadgeImageUrl,
      Rarity = request.Rarity,
      Category = request.Category,
      MaxProgress = request.MaxProgress,
      UnlockCriteria = request.UnlockCriteria,
      IsActive = true
    };

    var created = await _unitOfWork.BadgeRepository.CreateBadgeAsync(badge);
    return OperationResult<int>.SuccessResult(created.Id);
  }
}
