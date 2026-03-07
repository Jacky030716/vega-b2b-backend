using CleanArc.Domain.Entities.Social;

namespace CleanArc.Application.Contracts.Persistence;

public interface IFriendshipRepository
{
  Task<List<Friendship>> GetUserFriendsAsync(int userId);
  Task<List<Friendship>> GetPendingRequestsAsync(int userId);
  Task<Friendship> GetFriendshipAsync(int userId1, int userId2);
  Task<Friendship> SendFriendRequestAsync(Friendship friendship);
  Task UpdateFriendshipStatusAsync(int friendshipId, string status);
  Task DeleteFriendshipAsync(int friendshipId);
}
