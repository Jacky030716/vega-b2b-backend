using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Shop.Commands;

internal class EquipItemCommandHandler : IRequestHandler<EquipItemCommand, OperationResult<bool>>
{
  private readonly IUnitOfWork _unitOfWork;

  public EquipItemCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<bool>> Handle(EquipItemCommand request, CancellationToken cancellationToken)
  {
    await _unitOfWork.ShopRepository.EquipItemAsync(request.UserId, request.Category, request.ShopItemId);
    return OperationResult<bool>.SuccessResult(true);
  }
}
