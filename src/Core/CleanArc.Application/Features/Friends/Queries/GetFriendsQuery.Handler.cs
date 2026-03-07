using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Friends.Queries;

internal class GetFriendsQueryHandler : IRequestHandler<GetFriendsQuery, OperationResult<FriendsResult>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetFriendsQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<FriendsResult>> Handle(GetFriendsQuery request, CancellationToken cancellationToken)
  {
    var friends = await _unitOfWork.FriendshipRepository.GetUserFriendsAsync(request.UserId);
    var pending = await _unitOfWork.FriendshipRepository.GetPendingRequestsAsync(request.UserId);

    var friendDtos = friends.Select(f =>
    {
      var friendUser = f.RequesterId == request.UserId ? f.Addressee : f.Requester;
      return new FriendDto(friendUser.Id, friendUser.UserName, friendUser.Level, friendUser.AvatarId);
    }).ToList();

    var pendingDtos = pending.Select(p =>
        new FriendRequestDto(p.Id, p.RequesterId, p.Requester.UserName, p.CreatedTime)).ToList();

    return OperationResult<FriendsResult>.SuccessResult(new FriendsResult(friendDtos, pendingDtos));
  }
}
