using CleanArc.Domain.Entities.Mission;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class SpecialMissionConfiguration : IEntityTypeConfiguration<SpecialMission>
{
  public void Configure(EntityTypeBuilder<SpecialMission> builder)
  {
    builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
    builder.Property(m => m.MissionType).IsRequired().HasMaxLength(20);
    builder.Property(m => m.RewardType).IsRequired().HasMaxLength(20);
    builder.Property(m => m.IsActive).HasDefaultValue(true);
    builder.HasOne(m => m.RewardBadge).WithMany().HasForeignKey(m => m.RewardBadgeId).IsRequired(false);
  }
}
