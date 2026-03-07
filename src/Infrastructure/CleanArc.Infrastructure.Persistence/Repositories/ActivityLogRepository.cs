using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Activity;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class ActivityLogRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<ActivityLog>(dbContext), IActivityLogRepository
{
  public async Task<List<ActivityLog>> GetRecentActivityAsync(int userId, int count = 20)
  {
    return await TableNoTracking
        .Where(a => a.UserId == userId)
        .OrderByDescending(a => a.CreatedTime)
        .Take(count)
        .ToListAsync();
  }

  public async Task<ActivityLog> AddActivityAsync(ActivityLog activity)
  {
    await AddAsync(activity);
    await DbContext.SaveChangesAsync();
    return activity;
  }
}
