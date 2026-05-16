using CleanArc.Application.Features.Admin.Billing;
using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure.Billing;

public interface IBillingPaymentService
{
    Task<OperationResult<CheckoutSessionDto>> CreateCheckoutSessionAsync(
        int userId,
        string successUrl,
        string cancelUrl,
        decimal amount,
        string currency,
        string planId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> ProcessStripeWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);
}
