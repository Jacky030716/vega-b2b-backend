using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Shop.Commands;

public record CreateShopItemCommand(string Name, string Description, string Category, int Price, string Currency, string ImageUrl, string Rarity, int RequiredLevel, bool IsLimitedEdition, int? Stock)
    : IRequest<OperationResult<int>>;
