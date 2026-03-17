using CleanArc.Domain.Entities.Achievement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration;

public class UserAchievementEventConfiguration : IEntityTypeConfiguration<UserAchievementEvent>
{
  public void Configure(EntityTypeBuilder<UserAchievementEvent> builder)
  {
    builder.HasIndex(x => new { x.UserId, x.EventId }).IsUnique();

    builder.Property(x => x.EventType)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(x => x.EventId)
      .HasMaxLength(200)
      .IsRequired();

    builder.Property(x => x.PropertiesJson)
      .HasDefaultValue("{}");

    builder.HasOne(x => x.User)
        .WithMany()
        .HasForeignKey(x => x.UserId);
  }
}
