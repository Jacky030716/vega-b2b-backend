using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
  public void Configure(EntityTypeBuilder<QuizAttempt> builder)
  {
    builder.Property(a => a.AttemptId).IsRequired();
    builder.Property(a => a.QuizId).IsRequired();
    builder.Property(a => a.UserId).IsRequired();
    builder.Property(a => a.StartedAt).IsRequired();
    builder.Property(a => a.Mode).HasDefaultValue(string.Empty);
    builder.Property(a => a.ClientVersion).HasDefaultValue(string.Empty);

    builder.HasIndex(a => a.AttemptId).IsUnique();

    builder.HasMany(a => a.Answers)
        .WithOne(a => a.QuizAttempt)
        .HasForeignKey(a => a.QuizAttemptId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
