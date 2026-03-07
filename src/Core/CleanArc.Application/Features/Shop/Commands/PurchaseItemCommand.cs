using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Shop.Commands;

public record PurchaseItemCommand(int UserId, int ShopItemId) : IRequest<OperationResult<bool>>;
