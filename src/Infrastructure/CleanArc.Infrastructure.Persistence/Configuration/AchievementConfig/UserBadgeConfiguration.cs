using CleanArc.Domain.Entities.Achievement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
  public void Configure(EntityTypeBuilder<UserBadge> builder)
  {
    builder.HasIndex(ub => new { ub.UserId, ub.BadgeId }).IsUnique();
    builder.Property(ub => ub.Progress).HasDefaultValue(0);
    builder.Property(ub => ub.IsUnlocked).HasDefaultValue(false);
    builder.HasOne(ub => ub.User).WithMany().HasForeignKey(ub => ub.UserId);
    builder.HasOne(ub => ub.Badge).WithMany(b => b.UserBadges).HasForeignKey(ub => ub.BadgeId);
  }
}
