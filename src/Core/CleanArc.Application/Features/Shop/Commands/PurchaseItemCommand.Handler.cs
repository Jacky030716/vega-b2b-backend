using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Shop;
using Mediator;
using System.Text.Json;

namespace CleanArc.Application.Features.Shop.Commands;

internal class PurchaseItemCommandHandler : IRequestHandler<PurchaseItemCommand, OperationResult<bool>>
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IAppUserManager _userManager;
  private readonly IAchievementTrackingService _achievementTrackingService;

  public PurchaseItemCommandHandler(
    IUnitOfWork unitOfWork,
    IAppUserManager userManager,
    IAchievementTrackingService achievementTrackingService)
  {
    _unitOfWork = unitOfWork;
    _userManager = userManager;
    _achievementTrackingService = achievementTrackingService;
  }

  public async ValueTask<OperationResult<bool>> Handle(PurchaseItemCommand request, CancellationToken cancellationToken)
  {
    var shopItem = await _unitOfWork.ShopRepository.GetShopItemByIdAsync(request.ShopItemId);
    if (shopItem == null)
      return OperationResult<bool>.NotFoundResult("Shop item not found");

    if (!shopItem.IsAvailable)
      return OperationResult<bool>.FailureResult("This item is not currently available");

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

    if (shopItem.Currency == "diamonds" && shopItem.Price > 0)
    {
      await _achievementTrackingService.TrackEventAsync(
        request.UserId,
        "diamond_spent",
        $"diamond-spent:purchase:{request.UserId}:{shopItem.Id}:{DateTime.UtcNow:yyyyMMddHHmmssfff}",
        JsonSerializer.Serialize(new
        {
          amount = shopItem.Price,
          source = "shop_purchase",
          shopItemId = shopItem.Id,
          shopItemName = shopItem.Name,
          category = shopItem.Category,
          currency = shopItem.Currency,
        }),
        cancellationToken);
    }

    return OperationResult<bool>.SuccessResult(true);
  }
}
