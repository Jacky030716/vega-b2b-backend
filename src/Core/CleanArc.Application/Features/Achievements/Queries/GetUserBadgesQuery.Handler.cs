using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Queries;

internal class GetUserBadgesQueryHandler : IRequestHandler<GetUserBadgesQuery, OperationResult<List<UserBadgeDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetUserBadgesQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<UserBadgeDto>>> Handle(GetUserBadgesQuery request, CancellationToken cancellationToken)
  {
    var userBadges = await _unitOfWork.BadgeRepository.GetUserBadgesAsync(request.UserId);
    var result = userBadges.Select(ub => new UserBadgeDto(
        ub.Badge.Id, ub.Badge.Name, ub.Badge.Description, ub.Badge.ImageRef,
        ub.Badge.Category, ub.Badge.Rarity, ub.Badge.Requirement, ub.Badge.IsSecret,
        ub.EarnedAt, ub.IsFeatured, ub.SlotIndex,
        ub.Badge.RewardXp, ub.Badge.RewardDiamonds
    )).ToList();
    return OperationResult<List<UserBadgeDto>>.SuccessResult(result);
  }
}
