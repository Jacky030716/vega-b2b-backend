using CleanArc.Domain.Entities.Streak;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class DailyCheckInConfiguration : IEntityTypeConfiguration<DailyCheckIn>
{
  public void Configure(EntityTypeBuilder<DailyCheckIn> builder)
  {
    builder.Property(c => c.UserId).IsRequired();
    builder.Property(c => c.CheckInDate).IsRequired();
    builder.HasIndex(c => new { c.UserId, c.CheckInDate }).IsUnique();
    builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId);
  }
}
