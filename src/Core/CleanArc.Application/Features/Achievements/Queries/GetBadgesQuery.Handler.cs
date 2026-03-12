using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Queries;

internal class GetBadgesQueryHandler : IRequestHandler<GetBadgesQuery, OperationResult<List<BadgeDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetBadgesQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<BadgeDto>>> Handle(GetBadgesQuery request, CancellationToken cancellationToken)
  {
    var badges = await _unitOfWork.BadgeRepository.GetAllBadgesAsync();
    var result = badges.Select(b => new BadgeDto(
        b.Id, b.Name, b.Description, b.ImageRef,
        b.Category, b.Rarity, b.Requirement, b.IsSecret
    )).ToList();
    return OperationResult<List<BadgeDto>>.SuccessResult(result);
  }
}
