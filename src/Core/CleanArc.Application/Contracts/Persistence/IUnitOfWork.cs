namespace CleanArc.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    public IUserRefreshTokenRepository UserRefreshTokenRepository { get; }
    public IOrderRepository OrderRepository { get; }
    public IWordListRepository WordListRepository { get; }

    Task CommitAsync();
    ValueTask RollBackAsync();
}