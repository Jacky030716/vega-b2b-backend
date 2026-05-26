using CleanArc.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArc.Infrastructure.Persistence.Configuration.BillingConfig;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id").IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(40).IsRequired();
        builder.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(80).IsRequired();
        builder.Property(x => x.PlanId).HasColumnName("plan_id").HasMaxLength(80).HasDefaultValue("standard-monthly").IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
        builder.Property(x => x.StripePaymentIntentId).HasColumnName("stripe_payment_intent_id").HasMaxLength(160);
        builder.Property(x => x.StripeCheckoutSessionId).HasColumnName("stripe_checkout_session_id").HasMaxLength(160);
        builder.Property(x => x.IsDemo).HasColumnName("is_demo").HasDefaultValue(false);
        builder.Property(x => x.CreatedTime).HasColumnName("created_at");
        builder.Property(x => x.ModifiedDate).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.InstitutionId, x.CreatedTime });
        builder.HasIndex(x => x.StripePaymentIntentId);
        builder.HasIndex(x => x.StripeCheckoutSessionId);

        builder.HasOne(x => x.Institution)
            .WithMany()
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
