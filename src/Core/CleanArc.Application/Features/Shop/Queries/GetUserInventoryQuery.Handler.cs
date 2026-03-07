using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Shop.Queries;

internal class GetUserInventoryQueryHandler : IRequestHandler<GetUserInventoryQuery, OperationResult<UserInventoryResult>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetUserInventoryQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<UserInventoryResult>> Handle(GetUserInventoryQuery request, CancellationToken cancellationToken)
  {
    var items = await _unitOfWork.ShopRepository.GetUserInventoryAsync(request.UserId);
    var equipped = await _unitOfWork.ShopRepository.GetEquippedItemsAsync(request.UserId);

    var itemDtos = items.Select(i => new InventoryItemDto(
        i.Id, i.ShopItemId, i.ShopItem.Name, i.ShopItem.Category, i.ShopItem.ImageUrl, i.AcquiredAt)).ToList();

    var equippedDtos = equipped.Select(e => new EquippedItemDto(
        e.Category, e.ShopItemId, e.ShopItem.Name, e.ShopItem.ImageUrl)).ToList();

    return OperationResult<UserInventoryResult>.SuccessResult(new UserInventoryResult(itemDtos, equippedDtos));
  }
}
