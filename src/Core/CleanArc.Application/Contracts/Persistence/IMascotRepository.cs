using CleanArc.Domain.Entities.Mascot;

namespace CleanArc.Application.Contracts.Persistence;

public interface IMascotRepository
{
  Task<List<Mascot>> GetAllMascotsAsync();
  Task<Mascot> GetMascotByIdAsync(int mascotId);
  Task<Mascot> CreateMascotAsync(Mascot mascot);
  Task UpdateMascotAsync(Mascot mascot);
  Task DeleteMascotAsync(int mascotId);

  // User mascots
  Task<List<UserMascot>> GetUserMascotsAsync(int userId);
  Task<UserMascot> GetEquippedMascotAsync(int userId);
  Task<UserMascot> GetUserMascotAsync(int userId, int mascotId);
  Task<UserMascot> UnlockMascotAsync(UserMascot userMascot);
  Task EquipMascotAsync(int userId, int mascotId);
}
