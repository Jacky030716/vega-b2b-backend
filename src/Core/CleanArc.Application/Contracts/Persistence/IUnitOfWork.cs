namespace CleanArc.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    public IUserRefreshTokenRepository UserRefreshTokenRepository { get; }
    public IWordListRepository WordListRepository { get; }

    // New repositories
    public IStreakRepository StreakRepository { get; }
    public IShopRepository ShopRepository { get; }
    public IClassroomRepository ClassroomRepository { get; }
    public IProgressionRepository ProgressionRepository { get; }
    public IActivityLogRepository ActivityLogRepository { get; }
    public IChallengeRepository ChallengeRepository { get; }
    public IBadgeRepository BadgeRepository { get; }

    Task CommitAsync();
    ValueTask RollBackAsync();
}