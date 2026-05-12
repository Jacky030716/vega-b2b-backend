using CleanArc.Domain.Entities.Achievement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
  public void Configure(EntityTypeBuilder<Badge> builder)
  {
    builder.HasIndex(x => x.Code).IsUnique();

    builder.Property(x => x.Code)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(x => x.IsActive)
      .HasDefaultValue(true);
  }
}
