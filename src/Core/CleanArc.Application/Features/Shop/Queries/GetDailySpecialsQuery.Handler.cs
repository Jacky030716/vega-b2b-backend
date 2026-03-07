using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Shop.Queries;

internal class GetDailySpecialsQueryHandler : IRequestHandler<GetDailySpecialsQuery, OperationResult<List<DailySpecialDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetDailySpecialsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<DailySpecialDto>>> Handle(GetDailySpecialsQuery request, CancellationToken cancellationToken)
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var specials = await _unitOfWork.ShopRepository.GetDailySpecialsAsync(today);

    var result = specials.Select(s =>
    {
      var finalPrice = s.ShopItem.Price - (s.ShopItem.Price * s.DiscountPercent / 100);
      return new DailySpecialDto(s.Id, s.ShopItemId, s.ShopItem.Name, s.ShopItem.ImageUrl,
              s.ShopItem.Price, s.DiscountPercent, finalPrice, s.ShopItem.Currency);
    }).ToList();

    return OperationResult<List<DailySpecialDto>>.SuccessResult(result);
  }
}
