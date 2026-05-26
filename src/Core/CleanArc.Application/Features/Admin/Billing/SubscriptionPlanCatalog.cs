namespace CleanArc.Application.Features.Admin.Billing;

public static class SubscriptionPlanCatalog
{
    public const string DefaultPlanId = "standard-monthly";

    public static IReadOnlyList<SubscriptionPlanDto> Plans { get; } =
    [
        new(
            "standard-monthly",
            "Standard",
            "monthly",
            1990m,
            "MYR",
            "Essential classroom management for smaller institutions.",
            ["Up to 10 classrooms", "Up to 250 students", "Standard progress reports"]),
        new(
            "standard-annual",
            "Standard",
            "annual",
            19900m,
            "MYR",
            "Essential classroom management for smaller institutions.",
            ["Up to 10 classrooms", "Up to 250 students", "Standard progress reports"]),
        new(
            "premium-monthly",
            "Premium",
            "monthly",
            2990m,
            "MYR",
            "Advanced diagnostics and support for active learning programmes.",
            ["Unlimited classrooms", "Up to 1,000 students", "AI diagnostic auditing", "Priority support"]),
        new(
            "premium-annual",
            "Premium",
            "annual",
            29900m,
            "MYR",
            "Advanced diagnostics and support for active learning programmes.",
            ["Unlimited classrooms", "Up to 1,000 students", "AI diagnostic auditing", "Priority support"]),
    ];

    public static SubscriptionPlanDto? Find(string? planId)
    {
        if (string.IsNullOrWhiteSpace(planId))
            return null;

        return Plans.FirstOrDefault(x => x.Id.Equals(planId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static SubscriptionPlanDto ForExistingSubscription(string? planId, string? tier)
    {
        var plan = Find(planId);
        if (plan is not null)
            return plan;

        return string.Equals(tier, "Premium", StringComparison.OrdinalIgnoreCase)
            ? Find("premium-monthly")!
            : Find(DefaultPlanId)!;
    }
}
