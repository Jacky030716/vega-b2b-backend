using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Shop.Queries;

internal class GetShopItemsQueryHandler : IRequestHandler<GetShopItemsQuery, OperationResult<List<ShopItemDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetShopItemsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<ShopItemDto>>> Handle(GetShopItemsQuery request, CancellationToken cancellationToken)
  {
    var items = await _unitOfWork.ShopRepository.GetShopItemsAsync(request.Category);
    var result = items.Select(i => new ShopItemDto(
        i.Id, i.Name, i.Description, i.Category, i.Price, i.Currency,
        i.ImageUrl, i.Rarity, i.RequiredLevel, i.IsLimitedEdition, i.Stock)).ToList();
    return OperationResult<List<ShopItemDto>>.SuccessResult(result);
  }
}
