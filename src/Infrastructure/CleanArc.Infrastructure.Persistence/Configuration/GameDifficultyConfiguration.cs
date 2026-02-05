using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class GameDifficultyConfiguration : IEntityTypeConfiguration<GameDifficulty>
{
  public void Configure(EntityTypeBuilder<GameDifficulty> builder)
  {
    builder.Property(d => d.Level).IsRequired();
    builder.Property(d => d.Name).IsRequired();
    builder.Property(d => d.SequenceLength).IsRequired();
    builder.Property(d => d.Speed).IsRequired();
    builder.Property(d => d.Description).IsRequired();
  }
}
