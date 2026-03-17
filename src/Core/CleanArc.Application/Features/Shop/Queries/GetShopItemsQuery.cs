using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Shop.Queries;

public record GetShopItemsQuery(string? Category = null) : IRequest<OperationResult<List<ShopItemDto>>>;

public record ShopItemDto(int Id, string Name, string Description, string Category, string Theme, int Price, string Currency, string ImageUrl, string Rarity, int? RequiredLevel, bool IsLimitedEdition);
