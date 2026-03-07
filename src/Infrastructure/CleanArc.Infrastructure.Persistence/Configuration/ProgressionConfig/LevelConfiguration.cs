using CleanArc.Domain.Entities.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class LevelConfiguration : IEntityTypeConfiguration<Level>
{
  public void Configure(EntityTypeBuilder<Level> builder)
  {
    builder.Property(l => l.LevelNumber).IsRequired();
    builder.HasIndex(l => l.LevelNumber).IsUnique();
    builder.Property(l => l.Name).IsRequired().HasMaxLength(100);
    builder.Property(l => l.RequiredXP).IsRequired();
  }
}
