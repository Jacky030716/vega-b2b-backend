using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Social;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class FriendshipRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<Friendship>(dbContext), IFriendshipRepository
{
  public async Task<List<Friendship>> GetUserFriendsAsync(int userId)
  {
    return await TableNoTracking
        .Include(f => f.Requester)
        .Include(f => f.Addressee)
        .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == "accepted")
        .ToListAsync();
  }

  public async Task<List<Friendship>> GetPendingRequestsAsync(int userId)
  {
    return await TableNoTracking
        .Include(f => f.Requester)
        .Where(f => f.AddresseeId == userId && f.Status == "pending")
        .ToListAsync();
  }

  public async Task<Friendship> GetFriendshipAsync(int userId1, int userId2)
  {
    return await TableNoTracking
        .FirstOrDefaultAsync(f =>
            (f.RequesterId == userId1 && f.AddresseeId == userId2) ||
            (f.RequesterId == userId2 && f.AddresseeId == userId1));
  }

  public async Task<Friendship> SendFriendRequestAsync(Friendship friendship)
  {
    await AddAsync(friendship);
    await DbContext.SaveChangesAsync();
    return friendship;
  }

  public async Task UpdateFriendshipStatusAsync(int friendshipId, string status)
  {
    var friendship = await DbContext.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId);
    if (friendship != null)
    {
      friendship.Status = status;
      await DbContext.SaveChangesAsync();
    }
  }

  public async Task DeleteFriendshipAsync(int friendshipId)
  {
    var friendship = await DbContext.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId);
    if (friendship != null)
    {
      DbContext.Friendships.Remove(friendship);
      await DbContext.SaveChangesAsync();
    }
  }
}
