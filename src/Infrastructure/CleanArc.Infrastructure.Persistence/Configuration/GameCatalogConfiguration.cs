using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class GameCatalogConfiguration : IEntityTypeConfiguration<GameCatalog>
{
  public void Configure(EntityTypeBuilder<GameCatalog> builder)
  {
    builder.Property(c => c.Key).IsRequired();
    builder.Property(c => c.Label).IsRequired();
    builder.Property(c => c.QuestionType).IsRequired();

    builder.HasIndex(c => c.Key).IsUnique();
  }
}
