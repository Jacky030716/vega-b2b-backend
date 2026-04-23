using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Institution;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class InstitutionRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<Institution>(dbContext), IInstitutionRepository
{
    public async Task<Institution> GetInstitutionWithStatsAsync(int id)
    {
        return await DbContext.Institutions.AsNoTracking()
            .Include(i => i.Users)
            .FirstOrDefaultAsync(i => i.Id == id);
    }
}
