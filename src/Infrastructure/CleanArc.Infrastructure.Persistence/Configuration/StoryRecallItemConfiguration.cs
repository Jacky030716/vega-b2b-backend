using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class StoryRecallItemConfiguration : IEntityTypeConfiguration<StoryRecallItem>
{
  public void Configure(EntityTypeBuilder<StoryRecallItem> builder)
  {
    builder.Property(r => r.RecallQuestionId).IsRequired();
    builder.Property(r => r.QuestionText).IsRequired();
    builder.Property(r => r.CorrectAnswer).IsRequired();

    builder.HasMany(r => r.Options)
        .WithOne(o => o.StoryRecallItem)
        .HasForeignKey(o => o.StoryRecallItemId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
