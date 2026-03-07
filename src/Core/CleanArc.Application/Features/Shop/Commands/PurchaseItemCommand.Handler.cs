using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Shop;
using Mediator;

namespace CleanArc.Application.Features.Shop.Commands;

internal class PurchaseItemCommandHandler : IRequestHandler<PurchaseItemCommand, OperationResult<bool>>
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IAppUserManager _userManager;

  public PurchaseItemCommandHandler(IUnitOfWork unitOfWork, IAppUserManager userManager)
  {
    _unitOfWork = unitOfWork;
    _userManager = userManager;
  }

  public async ValueTask<OperationResult<bool>> Handle(PurchaseItemCommand request, CancellationToken cancellationToken)
  {
    var shopItem = await _unitOfWork.ShopRepository.GetShopItemByIdAsync(request.ShopItemId);
    if (shopItem == null)
      return OperationResult<bool>.NotFoundResult("Shop item not found");

    if (!shopItem.IsAvailable)
      return OperationResult<bool>.FailureResult("This item is not currently available");

    if (shopItem.Stock.HasValue && shopItem.Stock <= 0)
      return OperationResult<bool>.FailureResult("This item is out of stock");

    // Check user level requirement
    var user = await _userManager.GetUserByIdAsync(request.UserId);
    if (user == null)
      return OperationResult<bool>.NotFoundResult("User not found");

    if (user.Level < shopItem.RequiredLevel)
      return OperationResult<bool>.FailureResult($"You need to be level {shopItem.RequiredLevel} to purchase this item");

    // Check balance
    if (shopItem.Currency == "diamonds" && user.Diamonds < shopItem.Price)
      return OperationResult<bool>.FailureResult("Insufficient diamonds");

    // Deduct currency
    if (shopItem.Currency == "diamonds")
    {
      user.Diamonds -= shopItem.Price;
      await _userManager.UpdateUserAsync(user);
    }

    // Add to inventory
    await _unitOfWork.ShopRepository.AddToInventoryAsync(new UserInventoryItem
    {
      UserId = request.UserId,
      ShopItemId = request.ShopItemId,
      AcquiredAt = DateTime.UtcNow
    });

    // Record transaction
    await _unitOfWork.ShopRepository.AddDiamondTransactionAsync(new DiamondTransaction
    {
      UserId = request.UserId,
      Amount = -shopItem.Price,
      Reason = $"Purchased {shopItem.Name}",
      ReferenceId = shopItem.Id.ToString()
    });

    // Update stock
    if (shopItem.Stock.HasValue)
    {
      shopItem.Stock--;
      await _unitOfWork.ShopRepository.UpdateShopItemAsync(shopItem);
    }

    return OperationResult<bool>.SuccessResult(true);
  }
}
