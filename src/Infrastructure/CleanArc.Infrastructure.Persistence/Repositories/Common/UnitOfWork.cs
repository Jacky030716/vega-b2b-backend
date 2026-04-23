using CleanArc.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace CleanArc.Infrastructure.Persistence.Repositories.Common;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;

    public IUserRefreshTokenRepository UserRefreshTokenRepository { get; }
    public IWordListRepository WordListRepository { get; }

    // New repositories
    public IStreakRepository StreakRepository { get; }
    public IShopRepository ShopRepository { get; }
    public IClassroomRepository ClassroomRepository { get; }
    public IProgressionRepository ProgressionRepository { get; }
    public IActivityLogRepository ActivityLogRepository { get; }
    public IInstitutionRepository InstitutionRepository { get; }
    public IChallengeRepository ChallengeRepository { get; }
    public IBadgeRepository BadgeRepository { get; }
    public IStudentCredentialRepository StudentCredentialRepository { get; }
    public IVisualIconRepository VisualIconRepository { get; }
    public IStickerRepository StickerRepository { get; }

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
        UserRefreshTokenRepository = new UserRefreshTokenRepository(_db);
        WordListRepository = new WordListRepository(_db);

        // New repositories
        StreakRepository = new StreakRepository(_db);
        ShopRepository = new ShopRepository(_db);
        ClassroomRepository = new ClassroomRepository(_db);
        ProgressionRepository = new ProgressionRepository(_db);
        ActivityLogRepository = new ActivityLogRepository(_db);
        InstitutionRepository = new InstitutionRepository(_db);
        ChallengeRepository = new ChallengeRepository(_db);
        BadgeRepository = new BadgeRepository(_db);
        StudentCredentialRepository = new StudentCredentialRepository(_db);
        VisualIconRepository = new VisualIconRepository(_db);
        StickerRepository = new StickerRepository(_db);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        return _db.Database.BeginTransactionAsync(cancellationToken);
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