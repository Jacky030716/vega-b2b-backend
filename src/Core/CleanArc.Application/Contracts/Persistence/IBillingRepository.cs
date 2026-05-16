using CleanArc.Domain.Entities.Billing;

namespace CleanArc.Application.Contracts.Persistence;

public interface IBillingRepository
{
    Task<BillingAccount?> GetBillingAccountAsync(
        int institutionId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTransaction>> GetRecentTransactionsAsync(
        int institutionId,
        int count = 10,
        CancellationToken cancellationToken = default);

    Task AddBillingAccountAsync(BillingAccount account, CancellationToken cancellationToken = default);
    Task AddPaymentTransactionAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
}
