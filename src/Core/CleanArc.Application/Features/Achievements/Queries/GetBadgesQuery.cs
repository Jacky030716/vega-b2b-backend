using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Queries;

public record GetBadgesQuery() : IRequest<OperationResult<List<BadgeDto>>>;

/// <summary>Badge catalog item — no user-specific unlock state.</summary>
public record BadgeDto(
    int Id,
    string Name,
    string Description,
    /// <summary>Firebase Storage relative path, e.g. "badges/perfect_score.png"</summary>
    string ImageRef,
    string Category,
    string Rarity,
    string Requirement,
    bool IsSecret
);
