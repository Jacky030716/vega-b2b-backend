using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
  public void Configure(EntityTypeBuilder<QuizQuestion> builder)
  {
    builder.Property(q => q.QuestionId).IsRequired();
    builder.Property(q => q.Type).IsRequired();
    builder.Property(q => q.QuestionText).IsRequired();
    builder.Property(q => q.Explanation).HasDefaultValue(string.Empty);

    builder.HasIndex(q => new { q.QuizRefId, q.QuestionId }).IsUnique();

    builder.HasOne(q => q.MagicBackpackQuestion)
        .WithOne(m => m.QuizQuestion)
        .HasForeignKey<MagicBackpackQuestion>(m => m.QuizQuestionId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(q => q.WordBridgeQuestion)
        .WithOne(w => w.QuizQuestion)
        .HasForeignKey<WordBridgeQuestion>(w => w.QuizQuestionId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(q => q.StoryRecallQuestion)
        .WithOne(s => s.QuizQuestion)
        .HasForeignKey<StoryRecallQuestion>(s => s.QuizQuestionId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
