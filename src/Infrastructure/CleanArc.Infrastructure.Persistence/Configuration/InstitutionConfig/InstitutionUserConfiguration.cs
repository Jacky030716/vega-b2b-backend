using CleanArc.Domain.Entities.Institution;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.InstitutionConfig;

internal class InstitutionUserConfiguration : IEntityTypeConfiguration<InstitutionUser>
{
    public void Configure(EntityTypeBuilder<InstitutionUser> builder)
    {
        builder.ToTable("institution_users");

        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.InstitutionId).HasColumnName("institution_id").IsRequired();
        builder.Property(m => m.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(m => m.AccessScope)
            .HasColumnName("access_scope")
            .HasMaxLength(64)
            .HasDefaultValue("Member access")
            .IsRequired();
        builder.Property(m => m.IsPrimary)
            .HasColumnName("is_primary")
            .HasDefaultValue(true)
            .IsRequired();
        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();
        builder.Property(m => m.JoinedAt)
            .HasColumnName("joined_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        builder.Property(m => m.LeftAt).HasColumnName("left_at");

        builder.HasIndex(m => new { m.InstitutionId, m.IsActive })
            .HasDatabaseName("IX_institution_users_institution_active");
        builder.HasIndex(m => new { m.InstitutionId, m.UserId })
            .HasDatabaseName("IX_institution_users_active_pair")
            .IsUnique()
            .HasFilter("is_active = true");
        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("IX_institution_users_active_primary_user")
            .IsUnique()
            .HasFilter("is_primary = true AND is_active = true");

        builder.HasOne(m => m.Institution)
            .WithMany(i => i.UserMemberships)
            .HasForeignKey(m => m.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany(u => u.InstitutionMemberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
