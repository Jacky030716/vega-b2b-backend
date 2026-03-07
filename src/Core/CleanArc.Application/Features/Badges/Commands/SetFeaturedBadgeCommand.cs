using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Badges.Commands;

public record SetFeaturedBadgeCommand(int UserId, int BadgeId, int SlotIndex) : IRequest<OperationResult<bool>>;
