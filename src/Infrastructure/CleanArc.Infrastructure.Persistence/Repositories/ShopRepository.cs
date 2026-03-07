using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Shop;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class ShopRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<ShopItem>(dbContext), IShopRepository
{
  // Shop items
  public async Task<List<ShopItem>> GetShopItemsAsync(string category = null)
  {
    var query = TableNoTracking.Where(s => s.IsAvailable);
    if (!string.IsNullOrEmpty(category))
      query = query.Where(s => s.Category == category);
    return await query.ToListAsync();
  }

  public async Task<ShopItem> GetShopItemByIdAsync(int itemId)
  {
    return await TableNoTracking.FirstOrDefaultAsync(s => s.Id == itemId);
  }

  public async Task<ShopItem> CreateShopItemAsync(ShopItem item)
  {
    await AddAsync(item);
    await DbContext.SaveChangesAsync();
    return item;
  }

  public async Task UpdateShopItemAsync(ShopItem item)
  {
    DbContext.ShopItems.Update(item);
    await DbContext.SaveChangesAsync();
  }

  public async Task DeleteShopItemAsync(int itemId)
  {
    await DeleteAsync(s => s.Id == itemId);
  }

  // Inventory
  public async Task<List<UserInventoryItem>> GetUserInventoryAsync(int userId, string category = null)
  {
    var query = DbContext.UserInventoryItems.AsNoTracking()
        .Include(ui => ui.ShopItem)
        .Where(ui => ui.UserId == userId);
    if (!string.IsNullOrEmpty(category))
      query = query.Where(ui => ui.ShopItem.Category == category);
    return await query.ToListAsync();
  }

  public async Task<UserInventoryItem> GetUserInventoryItemAsync(int userId, int shopItemId)
  {
    return await DbContext.UserInventoryItems.AsNoTracking()
        .FirstOrDefaultAsync(ui => ui.UserId == userId && ui.ShopItemId == shopItemId);
  }

  public async Task<UserInventoryItem> AddToInventoryAsync(UserInventoryItem item)
  {
    DbContext.UserInventoryItems.Add(item);
    await DbContext.SaveChangesAsync();
    return item;
  }

  // Equipped items
  public async Task<List<UserEquippedItem>> GetUserEquippedItemsAsync(int userId)
  {
    return await DbContext.UserEquippedItems.AsNoTracking()
        .Include(ue => ue.ShopItem)
        .Where(ue => ue.UserId == userId)
        .ToListAsync();
  }

  public async Task EquipItemAsync(UserEquippedItem equipped)
  {
    var existing = await DbContext.UserEquippedItems
        .FirstOrDefaultAsync(ue => ue.UserId == equipped.UserId && ue.Category == equipped.Category);

    if (existing != null)
    {
      existing.ShopItemId = equipped.ShopItemId;
    }
    else
    {
      DbContext.UserEquippedItems.Add(equipped);
    }

    await DbContext.SaveChangesAsync();
  }

  public async Task UnequipItemAsync(int userId, string category)
  {
    var existing = await DbContext.UserEquippedItems
        .FirstOrDefaultAsync(ue => ue.UserId == userId && ue.Category == category);
    if (existing != null)
    {
      DbContext.UserEquippedItems.Remove(existing);
      await DbContext.SaveChangesAsync();
    }
  }

  public async Task<List<UserEquippedItem>> GetEquippedItemsAsync(int userId)
  {
    return await DbContext.UserEquippedItems.AsNoTracking()
        .Include(ue => ue.ShopItem)
        .Where(ue => ue.UserId == userId)
        .ToListAsync();
  }

  public async Task EquipItemAsync(int userId, string category, int shopItemId)
  {
    var existing = await DbContext.UserEquippedItems
        .FirstOrDefaultAsync(ue => ue.UserId == userId && ue.Category == category);
    if (existing != null)
    {
      existing.ShopItemId = shopItemId;
    }
    else
    {
      DbContext.UserEquippedItems.Add(new UserEquippedItem { UserId = userId, Category = category, ShopItemId = shopItemId });
    }
    await DbContext.SaveChangesAsync();
  }

  // Daily specials
  public async Task<DailySpecial> GetDailySpecialAsync(DateOnly date)
  {
    return await DbContext.DailySpecials.AsNoTracking()
        .Include(d => d.ShopItem)
        .FirstOrDefaultAsync(d => d.ActiveDate == date);
  }

  public async Task<DailySpecial> SetDailySpecialAsync(DailySpecial special)
  {
    var existing = await DbContext.DailySpecials
        .FirstOrDefaultAsync(d => d.ActiveDate == special.ActiveDate);
    if (existing != null)
    {
      existing.ShopItemId = special.ShopItemId;
      existing.DiscountPercent = special.DiscountPercent;
    }
    else
    {
      DbContext.DailySpecials.Add(special);
    }
    await DbContext.SaveChangesAsync();
    return existing ?? special;
  }

  public async Task<List<DailySpecial>> GetDailySpecialsAsync(DateOnly date)
  {
    return await DbContext.DailySpecials.AsNoTracking()
        .Include(d => d.ShopItem)
        .Where(d => d.ActiveDate == date)
        .ToListAsync();
  }

  // Diamond transactions
  public async Task<DiamondTransaction> AddDiamondTransactionAsync(DiamondTransaction transaction)
  {
    DbContext.DiamondTransactions.Add(transaction);
    await DbContext.SaveChangesAsync();
    return transaction;
  }

  public async Task<List<DiamondTransaction>> GetDiamondTransactionsAsync(int userId, int limit = 20)
  {
    return await DbContext.DiamondTransactions.AsNoTracking()
        .Where(d => d.UserId == userId)
        .OrderByDescending(d => d.CreatedTime)
        .Take(limit)
        .ToListAsync();
  }
}
