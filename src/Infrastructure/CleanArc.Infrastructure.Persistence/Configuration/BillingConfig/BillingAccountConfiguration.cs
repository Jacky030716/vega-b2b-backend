using CleanArc.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.BillingConfig;

public class BillingAccountConfiguration : IEntityTypeConfiguration<BillingAccount>
{
    public void Configure(EntityTypeBuilder<BillingAccount> builder)
    {
        builder.ToTable("billing_accounts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id").IsRequired();
        builder.Property(x => x.StripeCustomerId).HasColumnName("stripe_customer_id").HasMaxLength(160);
        builder.Property(x => x.PlanId).HasColumnName("plan_id").HasMaxLength(80).HasDefaultValue("standard-monthly");
        builder.Property(x => x.ActivePlanId).HasColumnName("active_plan_id").HasMaxLength(80).HasDefaultValue("standard-monthly");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(40).HasDefaultValue("NONE");
        builder.Property(x => x.CreatedTime).HasColumnName("created_at");
        builder.Property(x => x.ModifiedDate).HasColumnName("updated_at");

        builder.HasIndex(x => x.InstitutionId).IsUnique();
        builder.HasIndex(x => x.StripeCustomerId);

        builder.HasOne(x => x.Institution)
            .WithMany()
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
