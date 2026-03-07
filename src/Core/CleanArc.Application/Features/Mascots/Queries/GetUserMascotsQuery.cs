using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Mascots.Queries;

public record GetUserMascotsQuery(int UserId) : IRequest<OperationResult<UserMascotsResult>>;

public record UserMascotsResult(List<UserMascotDto> Mascots, int? EquippedMascotId);

public record UserMascotDto(int MascotId, string Name, string ImageUrl, bool IsEquipped, DateTime UnlockedAt);
