using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

public class VisualIconRepository : IVisualIconRepository
{
    private readonly ApplicationDbContext _dbContext;

    public VisualIconRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<VisualIcon>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.VisualIcons.AsNoTracking().ToListAsync(cancellationToken);
    }
}
