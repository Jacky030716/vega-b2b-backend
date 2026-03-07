using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Shop.Commands;

public record EquipItemCommand(int UserId, string Category, int ShopItemId) : IRequest<OperationResult<bool>>;
