using CleanArc.Domain.Entities.Mascot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class UserMascotConfiguration : IEntityTypeConfiguration<UserMascot>
{
  public void Configure(EntityTypeBuilder<UserMascot> builder)
  {
    builder.HasIndex(um => new { um.UserId, um.MascotId }).IsUnique();
    builder.Property(um => um.IsEquipped).HasDefaultValue(false);
    builder.HasOne(um => um.User).WithMany().HasForeignKey(um => um.UserId);
    builder.HasOne(um => um.Mascot).WithMany(m => m.UserMascots).HasForeignKey(um => um.MascotId);
  }
}
