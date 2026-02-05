using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class GameThemeItemConfiguration : IEntityTypeConfiguration<GameThemeItem>
{
  public void Configure(EntityTypeBuilder<GameThemeItem> builder)
  {
    builder.Property(i => i.ItemId).IsRequired();
    builder.Property(i => i.Name).IsRequired();
    builder.Property(i => i.Emoji).IsRequired();
  }
}
