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
    public IInstitutionRepository InstitutionRepository { get; }
    public IChallengeRepository ChallengeRepository { get; }
    public IBadgeRepository BadgeRepository { get; }
    public IStudentCredentialRepository StudentCredentialRepository { get; }
    public IVisualIconRepository VisualIconRepository { get; }
    public IStickerRepository StickerRepository { get; }

    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    Task CommitAsync();
    ValueTask RollBackAsync();
}