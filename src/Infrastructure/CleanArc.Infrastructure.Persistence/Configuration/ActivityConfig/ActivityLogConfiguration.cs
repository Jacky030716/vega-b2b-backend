using CleanArc.Domain.Entities.Activity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
  public void Configure(EntityTypeBuilder<ActivityLog> builder)
  {
    builder.Property(a => a.Type).IsRequired().HasMaxLength(30);
    builder.Property(a => a.Title).IsRequired().HasMaxLength(200);
    // Composite index covers WHERE UserId = ? ORDER BY CreatedTime DESC in GetRecentActivityAsync.
    builder.HasIndex(a => new { a.UserId, a.CreatedTime });
    builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId);
  }
}
