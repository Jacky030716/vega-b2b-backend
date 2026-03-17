using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Shop;
using Mediator;

namespace CleanArc.Application.Features.Shop.Commands;

internal class CreateShopItemCommandHandler : IRequestHandler<CreateShopItemCommand, OperationResult<int>>
{
  private readonly IUnitOfWork _unitOfWork;

  public CreateShopItemCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<int>> Handle(CreateShopItemCommand request, CancellationToken cancellationToken)
  {
    var item = new ShopItem
    {
      Name = request.Name,
      Description = request.Description,
      Category = request.Category,
      Theme = request.Theme,
      Price = request.Price,
      Currency = request.Currency,
      ImageUrl = request.ImageUrl,
      Rarity = request.Rarity,
      RequiredLevel = request.RequiredLevel,
      IsAvailable = true,
      IsLimitedEdition = request.IsLimitedEdition
    };

    var created = await _unitOfWork.ShopRepository.CreateShopItemAsync(item);
    return OperationResult<int>.SuccessResult(created.Id);
  }
}
