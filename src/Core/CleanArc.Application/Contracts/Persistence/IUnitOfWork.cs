namespace CleanArc.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    public IUserRefreshTokenRepository UserRefreshTokenRepository { get; }
    public IWordListRepository WordListRepository { get; }

    // New repositories
    public IStreakRepository StreakRepository { get; }
    public IBadgeRepository BadgeRepository { get; }
    public IShopRepository ShopRepository { get; }
    public IClassroomRepository ClassroomRepository { get; }
    public IProgressionRepository ProgressionRepository { get; }
    public IMissionRepository MissionRepository { get; }
    public IMascotRepository MascotRepository { get; }
    public IActivityLogRepository ActivityLogRepository { get; }
    public IFriendshipRepository FriendshipRepository { get; }

    Task CommitAsync();
    ValueTask RollBackAsync();
}