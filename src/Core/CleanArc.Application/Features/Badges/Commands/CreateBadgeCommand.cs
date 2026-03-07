using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Badges.Commands;

public record CreateBadgeCommand(string Name, string Description, string BadgeImageUrl, string Rarity, string Category, int MaxProgress, string? UnlockCriteria)
    : IRequest<OperationResult<int>>;
