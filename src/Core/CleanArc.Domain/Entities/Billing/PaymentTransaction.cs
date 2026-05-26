using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Billing;

public class PaymentTransaction : BaseEntity
{
    public int InstitutionId { get; set; }
    public CleanArc.Domain.Entities.Institution.Institution Institution { get; set; }
    public string Provider { get; set; } = "stripe";
    public string PaymentMethod { get; set; } = "card";
    public string PlanId { get; set; } = "standard-monthly";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MYR";
    public string Status { get; set; } = "NONE";
    public string StripePaymentIntentId { get; set; }
    public string StripeCheckoutSessionId { get; set; }
    public bool IsDemo { get; set; }
}
