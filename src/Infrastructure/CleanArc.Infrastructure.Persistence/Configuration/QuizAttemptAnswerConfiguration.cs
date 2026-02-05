using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class QuizAttemptAnswerConfiguration : IEntityTypeConfiguration<QuizAttemptAnswer>
{
  public void Configure(EntityTypeBuilder<QuizAttemptAnswer> builder)
  {
    builder.Property(a => a.QuestionId).IsRequired();
    builder.Property(a => a.QuestionType).IsRequired();

    builder.HasOne(a => a.MagicBackpackAttemptAnswer)
        .WithOne(m => m.QuizAttemptAnswer)
        .HasForeignKey<MagicBackpackAttemptAnswer>(m => m.QuizAttemptAnswerId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(a => a.WordBridgeAttemptAnswer)
        .WithOne(w => w.QuizAttemptAnswer)
        .HasForeignKey<WordBridgeAttemptAnswer>(w => w.QuizAttemptAnswerId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(a => a.StoryRecallAttemptAnswer)
        .WithOne(s => s.QuizAttemptAnswer)
        .HasForeignKey<StoryRecallAttemptAnswer>(s => s.QuizAttemptAnswerId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
