using CleanArc.Domain.Common;

namespace CleanArc.Domain.Entities.Billing;

public class BillingAccount : BaseEntity
{
    public int InstitutionId { get; set; }
    public CleanArc.Domain.Entities.Institution.Institution Institution { get; set; }
    public string StripeCustomerId { get; set; }
    public string PlanId { get; set; } = "standard-demo";
    public string Status { get; set; } = "NONE";
}
