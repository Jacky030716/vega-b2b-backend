using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Billing;

public sealed record GetBillingSummaryQuery(int UserId) : IRequest<OperationResult<BillingSummaryDto>>;

internal sealed class GetBillingSummaryQueryHandler(
    IUnitOfWork unitOfWork,
    IBillingRepository billingRepository)
    : IRequestHandler<GetBillingSummaryQuery, OperationResult<BillingSummaryDto>>
{
    public async ValueTask<OperationResult<BillingSummaryDto>> Handle(
        GetBillingSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
            request.UserId,
            cancellationToken);

        var institutionId = membership?.InstitutionId ?? 1;
        var institution = membership?.Institution
            ?? await unitOfWork.InstitutionRepository.GetInstitutionWithStatsAsync(institutionId);

        if (institution is null)
            return OperationResult<BillingSummaryDto>.NotFoundResult("Institution not found.");

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

        var dto = new BillingSummaryDto(
            CurrentPlan: string.IsNullOrWhiteSpace(institution.SubscriptionTier)
                ? "Standard"
                : institution.SubscriptionTier,
            PlanId: billingAccount?.PlanId ?? "standard-demo",
            RenewalDate: institution.RenewalDate,
            PaymentStatus: latestStatus,
            SavedPaymentMethod: savedPaymentMethod,
            DemoAmount: 299m,
            Currency: "MYR",
            PaymentMethods: GetPaymentMethods(),
            Transactions: transactions.Select(x => new PaymentTransactionDto(
                x.Id,
                x.Provider,
                x.PaymentMethod,
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
        new("touch-n-go", "Touch 'n Go eWallet", "wallet", "Demo Mode", "Demo Wallet", "Demo e-wallet option for Malaysian payment flow."),
        new("grabpay", "GrabPay", "wallet-outline", "Demo Mode", "Demo Wallet", "Demo wallet option for presentation checkout."),
    ];
}
