using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class GameConfigConfiguration : IEntityTypeConfiguration<GameConfig>
{
  public void Configure(EntityTypeBuilder<GameConfig> builder)
  {
    builder.Property(c => c.GameType).IsRequired();
    builder.Property(c => c.DefaultAgeGroup).IsRequired();
    builder.Property(c => c.DefaultThemeId).IsRequired();
    builder.Property(c => c.DefaultDifficulty).IsRequired();
    builder.Property(c => c.DefaultRounds).IsRequired();
    builder.Property(c => c.DefaultStartingDifficulty).IsRequired();

    builder.HasIndex(c => c.GameType).IsUnique();

    builder.HasMany(c => c.Themes)
        .WithOne(t => t.GameConfig)
        .HasForeignKey(t => t.GameConfigId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(c => c.DifficultyLevels)
        .WithOne(d => d.GameConfig)
        .HasForeignKey(d => d.GameConfigId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
