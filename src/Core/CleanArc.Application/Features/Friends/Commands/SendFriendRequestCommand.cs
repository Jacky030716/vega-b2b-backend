using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Friends.Commands;

public record SendFriendRequestCommand(int RequesterId, int AddresseeId) : IRequest<OperationResult<bool>>;
