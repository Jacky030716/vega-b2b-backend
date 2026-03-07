using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Shop.Queries;

public record GetUserInventoryQuery(int UserId) : IRequest<OperationResult<UserInventoryResult>>;

public record UserInventoryResult(List<InventoryItemDto> Items, List<EquippedItemDto> EquippedItems);

public record InventoryItemDto(int Id, int ShopItemId, string Name, string Category, string ImageUrl, DateTime AcquiredAt);

public record EquippedItemDto(string Category, int ShopItemId, string Name, string ImageUrl);
