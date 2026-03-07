using CleanArc.Domain.Entities.Shop;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class DailySpecialConfiguration : IEntityTypeConfiguration<DailySpecial>
{
  public void Configure(EntityTypeBuilder<DailySpecial> builder)
  {
    builder.HasIndex(d => d.ActiveDate).IsUnique();
    builder.Property(d => d.DiscountPercent).IsRequired();
    builder.HasOne(d => d.ShopItem).WithMany().HasForeignKey(d => d.ShopItemId);
  }
}
