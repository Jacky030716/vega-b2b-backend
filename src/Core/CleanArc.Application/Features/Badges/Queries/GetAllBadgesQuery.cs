using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Badges.Queries;

public record GetAllBadgesQuery() : IRequest<OperationResult<List<BadgeDto>>>;

public record BadgeDto(int Id, string Name, string Description, string BadgeImageUrl, string Rarity, string Category, int MaxProgress);
