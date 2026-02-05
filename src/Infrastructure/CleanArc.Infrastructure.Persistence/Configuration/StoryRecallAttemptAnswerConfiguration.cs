using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class StoryRecallAttemptAnswerConfiguration : IEntityTypeConfiguration<StoryRecallAttemptAnswer>
{
  public void Configure(EntityTypeBuilder<StoryRecallAttemptAnswer> builder)
  {
    builder.Property(a => a.Phase).IsRequired();

    builder.HasMany(a => a.Selections)
        .WithOne(s => s.StoryRecallAttemptAnswer)
        .HasForeignKey(s => s.StoryRecallAttemptAnswerId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
