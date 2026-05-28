#nullable enable

using System.Threading;
using System.Threading.Tasks;
using CleanArc.Application.Features.Admin.Billing;
using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure.Billing;

public interface IBillingPaymentService
{
    Task<OperationResult<CheckoutSessionDto>> CreateCheckoutSessionAsync(
        int userId,
        string successUrl,
        string cancelUrl,
        SubscriptionPlanDto plan,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> ProcessStripeWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> SyncPendingTransactionsAsync(
        int institutionId,
        CancellationToken cancellationToken = default);
}
