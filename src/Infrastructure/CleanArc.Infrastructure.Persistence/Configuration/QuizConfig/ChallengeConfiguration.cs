using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.QuizConfig;

public class ChallengeConfiguration : IEntityTypeConfiguration<Challenge>
{
  public void Configure(EntityTypeBuilder<Challenge> builder)
  {
    // Store game-specific content as PostgreSQL jsonb.
    // This gives native JSON indexing, filtering, and casting.
    // Each game type enforces its own schema via strongly-typed C# DTOs
    // (WordBridgeContent, WordTwinsContent, etc.) — no separate tables needed.
    builder.Property(c => c.ContentData)
        .HasColumnType("jsonb")
        .IsRequired();

    // Link the challenge to its classroom (nullable — null = global platform challenge)
    builder.HasOne(c => c.Classroom)
        .WithMany(cr => cr.Challenges)
        .HasForeignKey(c => c.ClassroomId)
        .OnDelete(DeleteBehavior.SetNull)
        .IsRequired(false);
  }
}
