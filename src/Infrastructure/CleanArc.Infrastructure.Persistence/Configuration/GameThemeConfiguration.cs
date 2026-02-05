using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class GameThemeConfiguration : IEntityTypeConfiguration<GameTheme>
{
  public void Configure(EntityTypeBuilder<GameTheme> builder)
  {
    builder.Property(t => t.ThemeId).IsRequired();
    builder.Property(t => t.Name).IsRequired();
    builder.Property(t => t.Emoji).IsRequired();
    builder.Property(t => t.Description).IsRequired();

    builder.HasMany(t => t.Items)
        .WithOne(i => i.GameTheme)
        .HasForeignKey(i => i.GameThemeId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(t => t.Gradients)
        .WithOne(g => g.GameTheme)
        .HasForeignKey(g => g.GameThemeId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
