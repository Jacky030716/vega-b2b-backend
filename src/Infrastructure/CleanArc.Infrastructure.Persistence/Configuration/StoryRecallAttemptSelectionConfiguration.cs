using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class StoryRecallAttemptSelectionConfiguration : IEntityTypeConfiguration<StoryRecallAttemptSelection>
{
  public void Configure(EntityTypeBuilder<StoryRecallAttemptSelection> builder)
  {
    builder.Property(s => s.RecallQuestionId).IsRequired();
    builder.Property(s => s.SelectedOption).IsRequired();
  }
}
