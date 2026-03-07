using CleanArc.Domain.Entities.Classroom;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class LeaderboardEntryConfiguration : IEntityTypeConfiguration<LeaderboardEntry>
{
  public void Configure(EntityTypeBuilder<LeaderboardEntry> builder)
  {
    builder.Property(le => le.QuizId).IsRequired();
    builder.Property(le => le.UserId).IsRequired();
    builder.HasIndex(le => new { le.QuizId, le.UserId, le.ClassroomId });
    builder.HasOne(le => le.User).WithMany().HasForeignKey(le => le.UserId);
    builder.HasOne(le => le.Classroom).WithMany().HasForeignKey(le => le.ClassroomId);
  }
}
