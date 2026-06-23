using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Billing;
using Mediator;

namespace CleanArc.Application.Features.Admin.Billing;

public sealed record CreateMockWalletPaymentCommand(
    int UserId,
    string PaymentMethod,
    string PlanId = SubscriptionPlanCatalog.DefaultPlanId) : IRequest<OperationResult<PaymentTransactionDto>>;

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

        var plan = SubscriptionPlanCatalog.Find(request.PlanId);
        if (plan is null)
        {
            return OperationResult<PaymentTransactionDto>.FailureResult(
                "The selected subscription plan is not available.");
        }

        var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
            request.UserId,
            cancellationToken);

        if (membership is null)
        {
            return OperationResult<PaymentTransactionDto>.ForbiddenResult(
                "Unable to resolve institution membership for this billing action.");
        }

        var institution = membership.Institution
            ?? await unitOfWork.InstitutionRepository.GetInstitutionWithStatsAsync(membership.InstitutionId);

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
                PlanId = plan.Id,
                ActivePlanId = plan.Id,
                Status = BillingStatus.DemoSucceeded,
            };
            await billingRepository.AddBillingAccountAsync(account, cancellationToken);
        }
        else
        {
            account.PlanId = plan.Id;
            account.ActivePlanId = plan.Id;
            account.Status = BillingStatus.DemoSucceeded;
        }

        var transaction = new PaymentTransaction
        {
            InstitutionId = institution.Id,
            Provider = "demo-wallet",
            PaymentMethod = method,
            PlanId = plan.Id,
            Amount = plan.Amount,
            Currency = plan.Currency,
            Status = BillingStatus.DemoSucceeded,
            IsDemo = true,
        };

        await billingRepository.AddPaymentTransactionAsync(transaction, cancellationToken);
        await unitOfWork.InstitutionRepository.UpdateSubscriptionAsync(
            institution.Id,
            plan.Name,
            plan.BillingInterval == "annual" ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1),
            cancellationToken);
        await unitOfWork.CommitAsync();

        return OperationResult<PaymentTransactionDto>.SuccessResult(new PaymentTransactionDto(
            transaction.Id,
            transaction.Provider,
            transaction.PaymentMethod,
            transaction.PlanId,
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
