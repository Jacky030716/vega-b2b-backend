using CleanArc.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.NotificationConfig;

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("user_notifications");
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).HasColumnName("body").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.AlertType).HasColumnName("alert_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(x => x.DeduplicationKey).HasColumnName("deduplication_key").HasMaxLength(160);
        builder.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ReadAt).HasColumnName("read_at");
        builder.Property(x => x.CreatedTime).HasColumnName("created_at");
        builder.Property(x => x.ModifiedDate).HasColumnName("updated_at");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.CreatedTime });
        builder.HasIndex(x => new { x.UserId, x.DeduplicationKey }).IsUnique();
    }
}
