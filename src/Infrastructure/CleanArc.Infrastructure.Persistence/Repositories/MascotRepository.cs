using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Mascot;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class MascotRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<Mascot>(dbContext), IMascotRepository
{
  // Mascots
  public async Task<List<Mascot>> GetAllMascotsAsync()
  {
    return await TableNoTracking.OrderBy(m => m.Name).ToListAsync();
  }

  public async Task<Mascot> GetMascotByIdAsync(int mascotId)
  {
    return await TableNoTracking.FirstOrDefaultAsync(m => m.Id == mascotId);
  }

  public async Task<Mascot> CreateMascotAsync(Mascot mascot)
  {
    await AddAsync(mascot);
    await DbContext.SaveChangesAsync();
    return mascot;
  }

  public async Task UpdateMascotAsync(Mascot mascot)
  {
    DbContext.Mascots.Update(mascot);
    await DbContext.SaveChangesAsync();
  }

  public async Task DeleteMascotAsync(int mascotId)
  {
    var mascot = await DbContext.Mascots.FirstOrDefaultAsync(m => m.Id == mascotId);
    if (mascot != null)
    {
      DbContext.Mascots.Remove(mascot);
      await DbContext.SaveChangesAsync();
    }
  }

  // User Mascots
  public async Task<List<UserMascot>> GetUserMascotsAsync(int userId)
  {
    return await DbContext.UserMascots.AsNoTracking()
        .Include(um => um.Mascot)
        .Where(um => um.UserId == userId)
        .ToListAsync();
  }

  public async Task<UserMascot> GetEquippedMascotAsync(int userId)
  {
    return await DbContext.UserMascots.AsNoTracking()
        .Include(um => um.Mascot)
        .FirstOrDefaultAsync(um => um.UserId == userId && um.IsEquipped);
  }

  public async Task<UserMascot> UnlockMascotAsync(UserMascot userMascot)
  {
    DbContext.UserMascots.Add(userMascot);
    await DbContext.SaveChangesAsync();
    return userMascot;
  }

  public async Task<UserMascot> GetUserMascotAsync(int userId, int mascotId)
  {
    return await DbContext.UserMascots.AsNoTracking()
        .Include(um => um.Mascot)
        .FirstOrDefaultAsync(um => um.UserId == userId && um.MascotId == mascotId);
  }

  public async Task EquipMascotAsync(int userId, int mascotId)
  {
    // Unequip current
    var current = await DbContext.UserMascots
        .Where(um => um.UserId == userId && um.IsEquipped)
        .ToListAsync();

    foreach (var um in current)
      um.IsEquipped = false;

    // Equip new
    var toEquip = await DbContext.UserMascots
        .FirstOrDefaultAsync(um => um.UserId == userId && um.MascotId == mascotId);

    if (toEquip != null)
      toEquip.IsEquipped = true;

    await DbContext.SaveChangesAsync();
  }
}
