using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Billing;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal sealed class BillingRepository(ApplicationDbContext dbContext)
    : BaseAsyncRepository<BillingAccount>(dbContext), IBillingRepository
{
    public Task<BillingAccount?> GetBillingAccountAsync(
        int institutionId,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = asTracking
            ? DbContext.BillingAccounts
            : DbContext.BillingAccounts.AsNoTracking();

        return query.FirstOrDefaultAsync(x => x.InstitutionId == institutionId, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentTransaction>> GetRecentTransactionsAsync(
        int institutionId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.PaymentTransactions
            .AsNoTracking()
            .Where(x => x.InstitutionId == institutionId)
            .OrderByDescending(x => x.CreatedTime)
            .Take(count <= 0 ? 10 : count)
            .ToListAsync(cancellationToken);
    }

    public Task<PaymentTransaction?> GetPendingCheckoutTransactionAsync(
        int institutionId,
        string checkoutSessionId,
        CancellationToken cancellationToken = default)
    {
        return DbContext.PaymentTransactions
            .FirstOrDefaultAsync(
                x => x.InstitutionId == institutionId
                     && x.StripeCheckoutSessionId == checkoutSessionId
                     && x.Status == CleanArc.Application.Features.Admin.Billing.BillingStatus.Pending,
                cancellationToken);
    }

    public async Task AddBillingAccountAsync(
        BillingAccount account,
        CancellationToken cancellationToken = default)
    {
        await DbContext.BillingAccounts.AddAsync(account, cancellationToken);
    }

    public async Task AddPaymentTransactionAsync(
        PaymentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await DbContext.PaymentTransactions.AddAsync(transaction, cancellationToken);
    }
}
