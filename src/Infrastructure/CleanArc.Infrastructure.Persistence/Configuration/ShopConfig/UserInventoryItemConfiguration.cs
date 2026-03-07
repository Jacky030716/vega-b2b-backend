using CleanArc.Domain.Entities.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class UserInventoryItemConfiguration : IEntityTypeConfiguration<UserInventoryItem>
{
  public void Configure(EntityTypeBuilder<UserInventoryItem> builder)
  {
    builder.HasIndex(ui => new { ui.UserId, ui.ShopItemId }).IsUnique();
    builder.HasOne(ui => ui.User).WithMany().HasForeignKey(ui => ui.UserId);
    builder.HasOne(ui => ui.ShopItem).WithMany(s => s.UserInventoryItems).HasForeignKey(ui => ui.ShopItemId);
  }
}
