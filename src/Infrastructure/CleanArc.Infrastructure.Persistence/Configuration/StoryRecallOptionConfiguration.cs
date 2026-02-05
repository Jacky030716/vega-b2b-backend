using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class StoryRecallOptionConfiguration : IEntityTypeConfiguration<StoryRecallOption>
{
  public void Configure(EntityTypeBuilder<StoryRecallOption> builder)
  {
    builder.Property(o => o.Order).IsRequired();
    builder.Property(o => o.Text).IsRequired();
  }
}
