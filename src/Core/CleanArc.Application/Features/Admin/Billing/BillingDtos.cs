namespace CleanArc.Application.Features.Admin.Billing;

public sealed record BillingSummaryDto(
    string CurrentPlan,
    string PlanId,
    DateTime RenewalDate,
    string PaymentStatus,
    string? SavedPaymentMethod,
    decimal DemoAmount,
    string Currency,
    IReadOnlyList<PaymentMethodOptionDto> PaymentMethods,
    IReadOnlyList<PaymentTransactionDto> Transactions);

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
    decimal Amount,
    string Currency,
    string Status,
    bool IsDemo,
    DateTime CreatedAt);

public sealed record CheckoutSessionDto(
    string CheckoutUrl,
    string SessionId,
    string Status);
