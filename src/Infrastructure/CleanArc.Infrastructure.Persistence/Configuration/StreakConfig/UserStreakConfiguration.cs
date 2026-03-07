using CleanArc.Domain.Entities.Streak;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class UserStreakConfiguration : IEntityTypeConfiguration<UserStreak>
{
  public void Configure(EntityTypeBuilder<UserStreak> builder)
  {
    builder.Property(c => c.UserId).IsRequired();
    builder.HasIndex(c => c.UserId).IsUnique();
    builder.Property(c => c.CurrentStreak).HasDefaultValue(0);
    builder.Property(c => c.BestStreak).HasDefaultValue(0);
    builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId);
  }
}
