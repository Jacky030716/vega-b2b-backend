using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
  public void Configure(EntityTypeBuilder<Quiz> builder)
  {
    builder.Property(q => q.QuizId).IsRequired();
    builder.HasIndex(q => q.QuizId).IsUnique();

    builder.Property(q => q.Title).IsRequired();
    builder.Property(q => q.Description).IsRequired();
    builder.Property(q => q.Theme).IsRequired();
    builder.Property(q => q.Type).IsRequired();
    builder.Property(q => q.Difficulty).IsRequired();
    builder.Property(q => q.CreatedAt).IsRequired();
    builder.Property(q => q.Category).IsRequired();
    builder.Property(q => q.ImageUrl).HasDefaultValue(string.Empty);

    builder.HasMany(q => q.Questions)
        .WithOne(q => q.Quiz)
        .HasForeignKey(q => q.QuizRefId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
