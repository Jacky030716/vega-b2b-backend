using CleanArc.Domain.Entities.Activity;

namespace CleanArc.Application.Contracts.Persistence;

public interface IActivityLogRepository
{
  Task<List<ActivityLog>> GetRecentActivityAsync(int userId, int limit = 10);
  Task<ActivityLog> AddActivityAsync(ActivityLog activity);
}
