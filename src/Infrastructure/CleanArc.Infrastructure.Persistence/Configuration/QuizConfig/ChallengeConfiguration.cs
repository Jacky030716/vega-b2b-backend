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

    builder.Property(c => c.StudentId).HasColumnName("student_id");
    builder.Property(c => c.ModuleId).HasColumnName("module_id");
    builder.Property(c => c.GameTemplateId).HasColumnName("game_template_id");
    builder.Property(c => c.ChallengeMode).HasColumnName("challenge_mode").HasMaxLength(50);
    builder.Property(c => c.SourceType).HasColumnName("source_type").HasMaxLength(50);
    builder.Property(c => c.ConfigJson).HasColumnName("config_json").HasColumnType("jsonb").HasDefaultValue("{}");
    builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(40).HasDefaultValue("assigned");
    builder.Property(c => c.AssignedAt).HasColumnName("assigned_at");
    builder.Property(c => c.DueAt).HasColumnName("due_at");

    // Link the challenge to its classroom (nullable — null = global platform challenge)
    builder.HasOne(c => c.Classroom)
        .WithMany(cr => cr.Challenges)
        .HasForeignKey(c => c.ClassroomId)
        .OnDelete(DeleteBehavior.SetNull)
        .IsRequired(false);

    builder.HasOne(c => c.Student)
        .WithMany()
        .HasForeignKey(c => c.StudentId)
        .OnDelete(DeleteBehavior.SetNull)
        .IsRequired(false);

    builder.HasOne(c => c.Module)
        .WithMany()
        .HasForeignKey(c => c.ModuleId)
        .OnDelete(DeleteBehavior.SetNull)
        .IsRequired(false);

    builder.HasOne(c => c.GameTemplate)
        .WithMany(gt => gt.Challenges)
        .HasForeignKey(c => c.GameTemplateId)
        .OnDelete(DeleteBehavior.SetNull)
        .IsRequired(false);
  }
}
