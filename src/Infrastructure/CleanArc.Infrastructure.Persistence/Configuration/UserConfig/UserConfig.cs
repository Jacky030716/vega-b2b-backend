using CleanArc.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.UserConfig;

internal class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "usr").Property(p => p.Id).HasColumnName("UserId");
        builder.Property(u => u.ExternalUuid).IsRequired();
        builder.HasIndex(u => u.ExternalUuid).IsUnique();
        builder.Property(u => u.ExpoPushToken).HasMaxLength(500);
        builder.Property(u => u.ReminderTimeLocal).HasMaxLength(5);
        builder.Property(u => u.QuietHoursStartLocal).HasMaxLength(5);
        builder.Property(u => u.QuietHoursEndLocal).HasMaxLength(5);
        builder.Property(u => u.NotificationTimezone).HasMaxLength(100);
        builder.Property(u => u.LastSrsNotificationSentAt);
    }
}
