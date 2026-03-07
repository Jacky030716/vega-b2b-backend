using CleanArc.Domain.Entities.Mascot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class MascotConfiguration : IEntityTypeConfiguration<Mascot>
{
  public void Configure(EntityTypeBuilder<Mascot> builder)
  {
    builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
    builder.HasIndex(m => m.Name).IsUnique();
    builder.Property(m => m.ImageUrl).IsRequired();
    builder.Property(m => m.IsDefault).HasDefaultValue(false);
  }
}
