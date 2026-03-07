using CleanArc.Domain.Entities.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class UserProgressConfiguration : IEntityTypeConfiguration<UserProgress>
{
  public void Configure(EntityTypeBuilder<UserProgress> builder)
  {
    builder.HasIndex(up => up.UserId).IsUnique();
    builder.Property(up => up.TotalXP).HasDefaultValue(0);
    builder.Property(up => up.CurrentLevel).HasDefaultValue(1);
    builder.Property(up => up.TotalQuizzesTaken).HasDefaultValue(0);
    builder.Property(up => up.TotalCorrectAnswers).HasDefaultValue(0);
    builder.Property(up => up.TotalTimePlayed).HasDefaultValue(0);
    builder.HasOne(up => up.User).WithMany().HasForeignKey(up => up.UserId);
  }
}
