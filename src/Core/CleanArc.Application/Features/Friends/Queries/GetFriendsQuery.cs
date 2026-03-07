using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Friends.Queries;

public record GetFriendsQuery(int UserId) : IRequest<OperationResult<FriendsResult>>;

public record FriendsResult(List<FriendDto> Friends, List<FriendRequestDto> PendingRequests);

public record FriendDto(int UserId, string UserName, int Level, string? AvatarId);

public record FriendRequestDto(int FriendshipId, int RequesterId, string RequesterName, DateTime RequestedAt);
