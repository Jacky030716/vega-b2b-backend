using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Queries;

public record GetUserBadgesQuery(int UserId) : IRequest<OperationResult<List<UserBadgeDto>>>;

/// <summary>Badge earned by the user — includes unlock state and featured info.</summary>
public record UserBadgeDto(
    int Id,
    string Name,
    string Description,
    string ImageRef,
    string Category,
    string Rarity,
    string Requirement,
    bool IsSecret,
    DateTime EarnedAt,
    bool IsFeatured,
    int? SlotIndex,
    int RewardXp,
    int RewardDiamonds
);
