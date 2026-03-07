using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Badges.Queries;

internal class GetUserBadgesQueryHandler : IRequestHandler<GetUserBadgesQuery, OperationResult<UserBadgesResult>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetUserBadgesQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<UserBadgesResult>> Handle(GetUserBadgesQuery request, CancellationToken cancellationToken)
  {
    var userBadges = await _unitOfWork.BadgeRepository.GetUserBadgesAsync(request.UserId);
    var featuredBadges = await _unitOfWork.BadgeRepository.GetFeaturedBadgesAsync(request.UserId);

    var badgeDtos = userBadges.Select(ub => new UserBadgeDto(
        ub.BadgeId, ub.Badge.Name, ub.Badge.BadgeImageUrl, ub.Badge.Rarity, ub.Badge.Category,
        ub.Progress, ub.Badge.MaxProgress, ub.IsUnlocked, ub.UnlockedDate)).ToList();

    var featuredDtos = featuredBadges.Select(fb => new FeaturedBadgeDto(
        fb.BadgeId, fb.Badge.Name, fb.Badge.BadgeImageUrl, fb.SlotIndex)).ToList();

    return OperationResult<UserBadgesResult>.SuccessResult(new UserBadgesResult(badgeDtos, featuredDtos));
  }
}
