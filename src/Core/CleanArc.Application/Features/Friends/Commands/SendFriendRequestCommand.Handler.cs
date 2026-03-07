using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Social;
using Mediator;

namespace CleanArc.Application.Features.Friends.Commands;

internal class SendFriendRequestCommandHandler : IRequestHandler<SendFriendRequestCommand, OperationResult<bool>>
{
  private readonly IUnitOfWork _unitOfWork;

  public SendFriendRequestCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<bool>> Handle(SendFriendRequestCommand request, CancellationToken cancellationToken)
  {
    if (request.RequesterId == request.AddresseeId)
      return OperationResult<bool>.FailureResult("Cannot send friend request to yourself");

    var existing = await _unitOfWork.FriendshipRepository.GetFriendshipAsync(request.RequesterId, request.AddresseeId);
    if (existing != null)
      return OperationResult<bool>.FailureResult("Friend request already exists or already friends");

    await _unitOfWork.FriendshipRepository.SendFriendRequestAsync(new Friendship
    {
      RequesterId = request.RequesterId,
      AddresseeId = request.AddresseeId,
      Status = "pending"
    });

    return OperationResult<bool>.SuccessResult(true);
  }
}
