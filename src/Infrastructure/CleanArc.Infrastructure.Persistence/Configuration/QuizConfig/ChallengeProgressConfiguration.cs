using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class ChallengeProgressConfiguration : IEntityTypeConfiguration<ChallengeProgress>
{
    public void Configure(EntityTypeBuilder<ChallengeProgress> builder)
    {
        // One row per (student + challenge + classroom)
        builder.HasIndex(cp => new { cp.UserId, cp.ChallengeId, cp.ClassroomId }).IsUnique();

        builder.Property(cp => cp.BestAccuracy).HasPrecision(5, 2);
        builder.Property(cp => cp.BestDurationSeconds).HasPrecision(10, 3);

        builder.HasOne(cp => cp.User)
            .WithMany()
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cp => cp.Challenge)
            .WithMany(c => c.Progresses)
            .HasForeignKey(cp => cp.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.Classroom)
            .WithMany()
            .HasForeignKey(cp => cp.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
