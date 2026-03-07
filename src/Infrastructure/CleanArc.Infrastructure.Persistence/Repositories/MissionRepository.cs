using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Mission;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class MissionRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<SpecialMission>(dbContext), IMissionRepository
{
  // Missions
  public async Task<List<SpecialMission>> GetActiveMissionsAsync()
  {
    var today = DateTime.UtcNow;
    return await TableNoTracking
        .Where(m => m.IsActive && m.StartDate <= today && m.EndDate >= today)
        .OrderBy(m => m.EndDate)
        .ToListAsync();
  }

  public async Task<SpecialMission> GetMissionByIdAsync(int missionId)
  {
    return await TableNoTracking.FirstOrDefaultAsync(m => m.Id == missionId);
  }

  public async Task<SpecialMission> CreateMissionAsync(SpecialMission mission)
  {
    await AddAsync(mission);
    await DbContext.SaveChangesAsync();
    return mission;
  }

  public async Task UpdateMissionAsync(SpecialMission mission)
  {
    DbContext.SpecialMissions.Update(mission);
    await DbContext.SaveChangesAsync();
  }

  public async Task DeleteMissionAsync(int missionId)
  {
    var mission = await DbContext.SpecialMissions.FirstOrDefaultAsync(m => m.Id == missionId);
    if (mission != null)
    {
      DbContext.SpecialMissions.Remove(mission);
      await DbContext.SaveChangesAsync();
    }
  }

  // User Missions
  public async Task<List<UserMission>> GetUserMissionsAsync(int userId)
  {
    return await DbContext.UserMissions.AsNoTracking()
        .Include(um => um.Mission)
        .Where(um => um.UserId == userId)
        .ToListAsync();
  }

  public async Task<UserMission> GetUserMissionAsync(int userId, int missionId)
  {
    return await DbContext.UserMissions.AsNoTracking()
        .Include(um => um.Mission)
        .FirstOrDefaultAsync(um => um.UserId == userId && um.MissionId == missionId);
  }

  public async Task<UserMission> StartMissionAsync(UserMission userMission)
  {
    DbContext.UserMissions.Add(userMission);
    await DbContext.SaveChangesAsync();
    return userMission;
  }

  public async Task<UserMission> CreateOrUpdateUserMissionAsync(UserMission userMission)
  {
    var existing = await DbContext.UserMissions
        .FirstOrDefaultAsync(u => u.UserId == userMission.UserId && u.MissionId == userMission.MissionId);
    if (existing != null)
    {
      existing.Progress = userMission.Progress;
      existing.IsCompleted = userMission.IsCompleted;
      existing.CompletedAt = userMission.CompletedAt;
      existing.ClaimedAt = userMission.ClaimedAt;
    }
    else
    {
      DbContext.UserMissions.Add(userMission);
    }
    await DbContext.SaveChangesAsync();
    return existing ?? userMission;
  }

  public async Task UpdateUserMissionProgressAsync(int userId, int missionId, int progress)
  {
    var um = await DbContext.UserMissions.FirstOrDefaultAsync(u => u.UserId == userId && u.MissionId == missionId);
    if (um != null)
    {
      um.Progress = progress;
      await DbContext.SaveChangesAsync();
    }
  }

  public async Task CompleteMissionAsync(int userId, int missionId)
  {
    var um = await DbContext.UserMissions.FirstOrDefaultAsync(u => u.UserId == userId && u.MissionId == missionId);
    if (um != null)
    {
      um.IsCompleted = true;
      um.CompletedAt = DateTime.UtcNow;
      await DbContext.SaveChangesAsync();
    }
  }

  public async Task ClaimMissionRewardAsync(int userId, int missionId)
  {
    var um = await DbContext.UserMissions.FirstOrDefaultAsync(u => u.UserId == userId && u.MissionId == missionId);
    if (um != null && um.IsCompleted && um.ClaimedAt == null)
    {
      um.ClaimedAt = DateTime.UtcNow;
      await DbContext.SaveChangesAsync();
    }
  }
}
