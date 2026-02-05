using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class StoryRecallQuestionConfiguration : IEntityTypeConfiguration<StoryRecallQuestion>
{
  public void Configure(EntityTypeBuilder<StoryRecallQuestion> builder)
  {
    builder.Property(s => s.Theme).IsRequired();
    builder.Property(s => s.StoryAudioUrl).IsRequired();
    builder.Property(s => s.StoryText).IsRequired();

    builder.HasMany(s => s.RecallQuestions)
        .WithOne(r => r.StoryRecallQuestion)
        .HasForeignKey(r => r.StoryRecallQuestionId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
