using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Friends.Commands;

internal class RespondFriendRequestCommandHandler : IRequestHandler<RespondFriendRequestCommand, OperationResult<bool>>
{
  private readonly IUnitOfWork _unitOfWork;

  public RespondFriendRequestCommandHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<bool>> Handle(RespondFriendRequestCommand request, CancellationToken cancellationToken)
  {
    var validActions = new[] { "accepted", "declined" };
    if (!validActions.Contains(request.Action.ToLower()))
      return OperationResult<bool>.FailureResult("Invalid action. Use 'accepted' or 'declined'");

    await _unitOfWork.FriendshipRepository.UpdateFriendshipStatusAsync(request.FriendshipId, request.Action.ToLower());
    return OperationResult<bool>.SuccessResult(true);
  }
}
