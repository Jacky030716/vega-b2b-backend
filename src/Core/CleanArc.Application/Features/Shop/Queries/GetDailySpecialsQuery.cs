using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Shop.Queries;

public record GetDailySpecialsQuery() : IRequest<OperationResult<List<DailySpecialDto>>>;

public record DailySpecialDto(int Id, int ShopItemId, string Name, string ImageUrl, int OriginalPrice, int DiscountPercent, int FinalPrice, string Currency);
