using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Badges.Queries;

public record GetUserBadgesQuery(int UserId) : IRequest<OperationResult<UserBadgesResult>>;

public record UserBadgesResult(List<UserBadgeDto> Badges, List<FeaturedBadgeDto> FeaturedBadges);

public record UserBadgeDto(int BadgeId, string Name, string BadgeImageUrl, string Rarity, string Category, int Progress, int MaxProgress, bool IsUnlocked, DateTime? UnlockedDate);

public record FeaturedBadgeDto(int BadgeId, string Name, string BadgeImageUrl, int SlotIndex);
