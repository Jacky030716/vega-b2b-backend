using CleanArc.Domain.Entities.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class UserEquippedItemConfiguration : IEntityTypeConfiguration<UserEquippedItem>
{
  public void Configure(EntityTypeBuilder<UserEquippedItem> builder)
  {
    builder.HasIndex(ue => new { ue.UserId, ue.Category }).IsUnique();
    builder.Property(ue => ue.Category).IsRequired().HasMaxLength(30);
    builder.HasOne(ue => ue.User).WithMany().HasForeignKey(ue => ue.UserId);
    builder.HasOne(ue => ue.ShopItem).WithMany().HasForeignKey(ue => ue.ShopItemId);
  }
}
