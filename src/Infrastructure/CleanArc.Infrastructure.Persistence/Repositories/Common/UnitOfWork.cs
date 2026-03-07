using CleanArc.Application.Contracts.Persistence;

namespace CleanArc.Infrastructure.Persistence.Repositories.Common;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;

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

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
        UserRefreshTokenRepository = new UserRefreshTokenRepository(_db);
        WordListRepository = new WordListRepository(_db);

        // New repositories
        StreakRepository = new StreakRepository(_db);
        BadgeRepository = new BadgeRepository(_db);
        ShopRepository = new ShopRepository(_db);
        ClassroomRepository = new ClassroomRepository(_db);
        ProgressionRepository = new ProgressionRepository(_db);
        MissionRepository = new MissionRepository(_db);
        MascotRepository = new MascotRepository(_db);
        ActivityLogRepository = new ActivityLogRepository(_db);
        FriendshipRepository = new FriendshipRepository(_db);
    }

    public Task CommitAsync()
    {
        return _db.SaveChangesAsync();
    }

    public ValueTask RollBackAsync()
    {
        return _db.DisposeAsync();
    }
}