using CleanArc.Domain.Entities.Mission;

namespace CleanArc.Application.Contracts.Persistence;

public interface IMissionRepository
{
  Task<List<SpecialMission>> GetActiveMissionsAsync();
  Task<SpecialMission> GetMissionByIdAsync(int missionId);
  Task<SpecialMission> CreateMissionAsync(SpecialMission mission);
  Task UpdateMissionAsync(SpecialMission mission);
  Task DeleteMissionAsync(int missionId);

  // User missions
  Task<List<UserMission>> GetUserMissionsAsync(int userId);
  Task<UserMission> GetUserMissionAsync(int userId, int missionId);
  Task<UserMission> CreateOrUpdateUserMissionAsync(UserMission userMission);
  Task ClaimMissionRewardAsync(int userId, int missionId);
}
