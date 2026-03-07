using CleanArc.Domain.Entities.Achievement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class FeaturedBadgeConfiguration : IEntityTypeConfiguration<FeaturedBadge>
{
  public void Configure(EntityTypeBuilder<FeaturedBadge> builder)
  {
    builder.HasIndex(fb => new { fb.UserId, fb.SlotIndex }).IsUnique();
    builder.HasOne(fb => fb.User).WithMany().HasForeignKey(fb => fb.UserId);
    builder.HasOne(fb => fb.Badge).WithMany().HasForeignKey(fb => fb.BadgeId);
  }
}
