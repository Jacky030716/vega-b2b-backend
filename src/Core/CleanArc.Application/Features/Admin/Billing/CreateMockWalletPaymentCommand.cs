using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Billing;
using Mediator;

namespace CleanArc.Application.Features.Admin.Billing;

public sealed record CreateMockWalletPaymentCommand(
    int UserId,
    string PaymentMethod,
    decimal Amount = 299m,
    string Currency = "MYR") : IRequest<OperationResult<PaymentTransactionDto>>;

internal sealed class CreateMockWalletPaymentCommandHandler(
    IUnitOfWork unitOfWork,
    IBillingRepository billingRepository)
    : IRequestHandler<CreateMockWalletPaymentCommand, OperationResult<PaymentTransactionDto>>
{
    public async ValueTask<OperationResult<PaymentTransactionDto>> Handle(
        CreateMockWalletPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var method = NormalizeMethod(request.PaymentMethod);
        if (method is null)
        {
            return OperationResult<PaymentTransactionDto>.FailureResult(
                "Unsupported demo wallet payment method.");
        }

        var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
            request.UserId,
            cancellationToken);
        var institutionId = membership?.InstitutionId ?? 1;
        var institution = membership?.Institution
            ?? await unitOfWork.InstitutionRepository.GetInstitutionWithStatsAsync(institutionId);

        if (institution is null)
            return OperationResult<PaymentTransactionDto>.NotFoundResult("Institution not found.");

        var account = await billingRepository.GetBillingAccountAsync(
            institution.Id,
            asTracking: true,
            cancellationToken);
        if (account is null)
        {
            account = new BillingAccount
            {
                InstitutionId = institution.Id,
                PlanId = "standard-demo",
                Status = BillingStatus.DemoSucceeded,
            };
            await billingRepository.AddBillingAccountAsync(account, cancellationToken);
        }
        else
        {
            account.Status = BillingStatus.DemoSucceeded;
        }

        var transaction = new PaymentTransaction
        {
            InstitutionId = institution.Id,
            Provider = "demo-wallet",
            PaymentMethod = method,
            Amount = request.Amount <= 0 ? 299m : request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency)
                ? "MYR"
                : request.Currency.Trim().ToUpperInvariant(),
            Status = BillingStatus.DemoSucceeded,
            IsDemo = true,
        };

        await billingRepository.AddPaymentTransactionAsync(transaction, cancellationToken);
        await unitOfWork.CommitAsync();

        return OperationResult<PaymentTransactionDto>.SuccessResult(new PaymentTransactionDto(
            transaction.Id,
            transaction.Provider,
            transaction.PaymentMethod,
            transaction.Amount,
            transaction.Currency,
            transaction.Status,
            transaction.IsDemo,
            transaction.CreatedTime));
    }

    private static string? NormalizeMethod(string paymentMethod)
    {
        var normalized = paymentMethod?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "touch-n-go" or "touchngo" or "tng" => "touch-n-go",
            "grabpay" => "grabpay",
            _ => null,
        };
    }
}
