namespace CleanArc.Application.Features.Admin.Billing;

public sealed record BillingSummaryDto(
    string CurrentPlan,
    string CurrentPlanId,
    string SelectedPlanId,
    string BillingInterval,
    DateTime RenewalDate,
    string PaymentStatus,
    string? SavedPaymentMethod,
    decimal Amount,
    string Currency,
    IReadOnlyList<SubscriptionPlanDto> AvailablePlans,
    IReadOnlyList<PaymentMethodOptionDto> PaymentMethods,
    IReadOnlyList<PaymentTransactionDto> Transactions);

public sealed record SubscriptionPlanDto(
    string Id,
    string Name,
    string BillingInterval,
    decimal Amount,
    string Currency,
    string Description,
    IReadOnlyList<string> Features);

public sealed record PaymentMethodOptionDto(
    string Id,
    string Name,
    string Icon,
    string Status,
    string Label,
    string HelperText);

public sealed record PaymentTransactionDto(
    int Id,
    string Provider,
    string PaymentMethod,
    string PlanId,
    decimal Amount,
    string Currency,
    string Status,
    bool IsDemo,
    DateTime CreatedAt,
    string? StripeCheckoutSessionId = null);

public sealed record CheckoutSessionDto(
    string CheckoutUrl,
    string SessionId,
    string Status);
