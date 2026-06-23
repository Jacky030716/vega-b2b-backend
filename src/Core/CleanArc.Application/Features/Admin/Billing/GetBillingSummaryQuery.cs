using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Contracts.Infrastructure.Billing;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Billing;

public sealed record GetBillingSummaryQuery(int UserId) : IRequest<OperationResult<BillingSummaryDto>>;

internal sealed class GetBillingSummaryQueryHandler(
    IUnitOfWork unitOfWork,
    IBillingRepository billingRepository,
    IBillingPaymentService billingPaymentService)
    : IRequestHandler<GetBillingSummaryQuery, OperationResult<BillingSummaryDto>>
{
    public async ValueTask<OperationResult<BillingSummaryDto>> Handle(
        GetBillingSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
            request.UserId,
            cancellationToken);

        if (membership is null)
        {
            return OperationResult<BillingSummaryDto>.ForbiddenResult(
                "Unable to resolve institution membership for this billing action.");
        }

        var institution = membership.Institution
            ?? await unitOfWork.InstitutionRepository.GetInstitutionWithStatsAsync(membership.InstitutionId);

        if (institution is null)
            return OperationResult<BillingSummaryDto>.NotFoundResult("Institution not found.");

        // Sync pending transactions from Stripe before retrieving summary
        try
        {
            await billingPaymentService.SyncPendingTransactionsAsync(institution.Id, cancellationToken);
        }
        catch (System.Exception)
        {
            // Fail-safe to ensure query still succeeds if Stripe API is down
        }

        var billingAccount = await billingRepository.GetBillingAccountAsync(
            institution.Id,
            cancellationToken: cancellationToken);
        var transactions = await billingRepository.GetRecentTransactionsAsync(
            institution.Id,
            10,
            cancellationToken);

        var latestStatus = transactions.FirstOrDefault()?.Status
            ?? billingAccount?.Status
            ?? BillingStatus.None;

        var savedPaymentMethod = transactions
            .FirstOrDefault(x => x.Status is BillingStatus.Succeeded or BillingStatus.DemoSucceeded)
            ?.PaymentMethod;
        var activePlan = SubscriptionPlanCatalog.ForExistingSubscription(
            billingAccount?.ActivePlanId,
            institution.SubscriptionTier);
        var selectedPlan = SubscriptionPlanCatalog.ForExistingSubscription(
            billingAccount?.PlanId,
            institution.SubscriptionTier);

        var dto = new BillingSummaryDto(
            CurrentPlan: activePlan.Name,
            CurrentPlanId: activePlan.Id,
            SelectedPlanId: selectedPlan.Id,
            BillingInterval: activePlan.BillingInterval,
            RenewalDate: institution.RenewalDate,
            PaymentStatus: latestStatus,
            SavedPaymentMethod: savedPaymentMethod,
            Amount: activePlan.Amount,
            Currency: activePlan.Currency,
            AvailablePlans: SubscriptionPlanCatalog.Plans,
            PaymentMethods: GetPaymentMethods(),
            Transactions: transactions.Select(x => new PaymentTransactionDto(
                x.Id,
                x.Provider,
                x.PaymentMethod,
                x.PlanId,
                x.Amount,
                x.Currency,
                x.Status,
                x.IsDemo,
                x.CreatedTime)).ToArray());

        return OperationResult<BillingSummaryDto>.SuccessResult(dto);
    }

    private static IReadOnlyList<PaymentMethodOptionDto> GetPaymentMethods() =>
    [
        new("card", "Credit / debit card", "credit-card", "Available", "Stripe Test Mode", "Use Stripe test cards for demo checkout."),
        new("wallet", "Apple Pay / Google Pay", "cellphone-nfc", "Demo Mode", "Stripe Wallet", "Wallet visibility depends on Stripe Checkout and device setup."),
        new("fpx", "FPX Online Banking", "bank", "Available", "Stripe Test Mode", "Malaysian online banking (test mode) to support local payment routing."),
        new("grabpay", "GrabPay", "wallet-outline", "Available", "Stripe Test Mode", "GrabPay wallet (test mode) to support local mobile e-wallet payments."),
    ];
}
