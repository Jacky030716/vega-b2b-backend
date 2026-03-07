using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Friends.Commands;

public record RespondFriendRequestCommand(int UserId, int FriendshipId, string Action) : IRequest<OperationResult<bool>>;
