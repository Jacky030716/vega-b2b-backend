using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class GameThemeGradientConfiguration : IEntityTypeConfiguration<GameThemeGradient>
{
  public void Configure(EntityTypeBuilder<GameThemeGradient> builder)
  {
    builder.Property(g => g.Color).IsRequired();
  }
}
