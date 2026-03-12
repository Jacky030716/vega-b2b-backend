using CleanArc.Domain.Entities.Achievement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
  public void Configure(EntityTypeBuilder<UserBadge> builder)
  {
    // A user can only earn the same badge once
    builder.HasIndex(ub => new { ub.UserId, ub.BadgeId }).IsUnique();

    builder.HasOne(ub => ub.User)
           .WithMany()
           .HasForeignKey(ub => ub.UserId);

    builder.HasOne(ub => ub.Badge)
           .WithMany(b => b.UserBadges)
           .HasForeignKey(ub => ub.BadgeId);
  }
}
