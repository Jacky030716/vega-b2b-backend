using CleanArc.Domain.Entities.User;

namespace CleanArc.Application.Contracts.Persistence;

public interface IVisualIconRepository
{
    Task<List<VisualIcon>> GetAllAsync(CancellationToken cancellationToken);
}
