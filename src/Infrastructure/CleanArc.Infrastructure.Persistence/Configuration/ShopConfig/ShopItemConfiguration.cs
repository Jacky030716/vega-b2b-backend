using CleanArc.Domain.Entities.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class ShopItemConfiguration : IEntityTypeConfiguration<ShopItem>
{
  public void Configure(EntityTypeBuilder<ShopItem> builder)
  {
    builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
    builder.Property(s => s.Category).IsRequired().HasMaxLength(30);
    builder.Property(s => s.Price).IsRequired();
    builder.Property(s => s.Currency).IsRequired().HasMaxLength(20).HasDefaultValue("diamonds");
    builder.Property(s => s.Rarity).HasMaxLength(20);
    builder.Property(s => s.IsAvailable).HasDefaultValue(true);
    builder.Property(s => s.IsLimitedEdition).HasDefaultValue(false);
  }
}
