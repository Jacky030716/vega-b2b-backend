using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Badges.Queries;

internal class GetAllBadgesQueryHandler : IRequestHandler<GetAllBadgesQuery, OperationResult<List<BadgeDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetAllBadgesQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<BadgeDto>>> Handle(GetAllBadgesQuery request, CancellationToken cancellationToken)
  {
    var badges = await _unitOfWork.BadgeRepository.GetAllBadgesAsync();
    var result = badges.Select(b => new BadgeDto(b.Id, b.Name, b.Description, b.BadgeImageUrl, b.Rarity, b.Category, b.MaxProgress)).ToList();
    return OperationResult<List<BadgeDto>>.SuccessResult(result);
  }
}
