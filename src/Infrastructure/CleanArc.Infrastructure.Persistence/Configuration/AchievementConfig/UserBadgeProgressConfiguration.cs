using CleanArc.Domain.Entities.Achievement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class UserBadgeProgressConfiguration : IEntityTypeConfiguration<UserBadgeProgress>
{
  public void Configure(EntityTypeBuilder<UserBadgeProgress> builder)
  {
    builder.HasIndex(x => new { x.UserId, x.BadgeId }).IsUnique();

    builder.Property(x => x.ProgressValue)
        .HasPrecision(18, 4)
        .HasDefaultValue(0m);

    builder.HasOne(x => x.User)
        .WithMany()
        .HasForeignKey(x => x.UserId);

    builder.HasOne(x => x.Badge)
        .WithMany(x => x.UserBadgeProgresses)
        .HasForeignKey(x => x.BadgeId);
  }
}
