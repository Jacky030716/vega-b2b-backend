using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.QuizConfig;

public class GameEnemyConfiguration : IEntityTypeConfiguration<GameEnemy>
{
    public void Configure(EntityTypeBuilder<GameEnemy> builder)
    {
        builder.Property(e => e.Key).IsRequired().HasMaxLength(80);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(120);
        builder.Property(e => e.ImageRef).IsRequired().HasMaxLength(500);
        builder.Property(e => e.DisplayOrder).HasDefaultValue(0);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasIndex(e => new { e.IsActive, e.DisplayOrder });
    }
}
