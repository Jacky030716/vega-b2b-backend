using CleanArc.Domain.Entities.Achievement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
  public void Configure(EntityTypeBuilder<Badge> builder)
  {
    builder.Property(b => b.Name).IsRequired().HasMaxLength(100);
    builder.Property(b => b.Description).IsRequired().HasMaxLength(500);
    builder.Property(b => b.Rarity).IsRequired().HasMaxLength(20);
    builder.Property(b => b.Category).IsRequired().HasMaxLength(20);
    builder.Property(b => b.IsActive).HasDefaultValue(true);
    builder.HasIndex(b => b.Name).IsUnique();
  }
}
