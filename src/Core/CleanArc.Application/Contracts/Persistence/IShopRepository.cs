using CleanArc.Domain.Entities.Shop;

namespace CleanArc.Application.Contracts.Persistence;

public interface IShopRepository
{
  // Shop items
  Task<List<ShopItem>> GetShopItemsAsync(string category = null);
  Task<List<ShopItem>> GetShopItemsByCategoryAndRaritiesAsync(string category, params string[] rarities);
  Task<ShopItem> GetShopItemByIdAsync(int itemId);
  Task<ShopItem> CreateShopItemAsync(ShopItem item);
  Task UpdateShopItemAsync(ShopItem item);
  Task DeleteShopItemAsync(int itemId);

  // Inventory
  Task<List<UserInventoryItem>> GetUserInventoryAsync(int userId, string category = null);
  Task<UserInventoryItem> GetUserInventoryItemAsync(int userId, int shopItemId);
  Task<UserInventoryItem> AddToInventoryAsync(UserInventoryItem item);

  // Equipped items
  Task<List<UserEquippedItem>> GetUserEquippedItemsAsync(int userId);
  Task<List<UserEquippedItem>> GetEquippedItemsAsync(int userId);
  Task EquipItemAsync(UserEquippedItem equipped);
  Task EquipItemAsync(int userId, string category, int shopItemId);
  Task UnequipItemAsync(int userId, string category);

  // Daily specials
  Task<DailySpecial> GetDailySpecialAsync(DateOnly date);
  Task<List<DailySpecial>> GetDailySpecialsAsync(DateOnly date);
  Task<DailySpecial> SetDailySpecialAsync(DailySpecial special);

  // Diamond transactions
  Task<DiamondTransaction> AddDiamondTransactionAsync(DiamondTransaction transaction);
  Task<List<DiamondTransaction>> GetDiamondTransactionsAsync(int userId, int limit = 20);
}
