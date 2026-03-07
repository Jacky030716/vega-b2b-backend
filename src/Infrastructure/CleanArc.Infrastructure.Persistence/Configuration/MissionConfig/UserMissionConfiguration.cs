using CleanArc.Domain.Entities.Mission;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class UserMissionConfiguration : IEntityTypeConfiguration<UserMission>
{
  public void Configure(EntityTypeBuilder<UserMission> builder)
  {
    builder.HasIndex(um => new { um.UserId, um.MissionId }).IsUnique();
    builder.Property(um => um.Progress).HasDefaultValue(0);
    builder.Property(um => um.IsCompleted).HasDefaultValue(false);
    builder.HasOne(um => um.User).WithMany().HasForeignKey(um => um.UserId);
    builder.HasOne(um => um.Mission).WithMany(m => m.UserMissions).HasForeignKey(um => um.MissionId);
  }
}
