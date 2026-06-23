using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Billing;

public sealed record CancelCheckoutSessionCommand(
    int UserId,
    string SessionId) : IRequest<OperationResult<PaymentTransactionDto>>;

internal sealed class CancelCheckoutSessionCommandHandler(
    IUnitOfWork unitOfWork,
    IBillingRepository billingRepository)
    : IRequestHandler<CancelCheckoutSessionCommand, OperationResult<PaymentTransactionDto>>
{
    public async ValueTask<OperationResult<PaymentTransactionDto>> Handle(
        CancelCheckoutSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return OperationResult<PaymentTransactionDto>.FailureResult(
                "Checkout session id is required.");
        }

        var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
            request.UserId,
            cancellationToken);

        if (membership is null)
        {
            return OperationResult<PaymentTransactionDto>.ForbiddenResult(
                "Unable to resolve institution membership for this billing action.");
        }

        var transaction = await billingRepository.GetPendingCheckoutTransactionAsync(
            membership.InstitutionId,
            request.SessionId.Trim(),
            cancellationToken);

        if (transaction is null)
        {
            return OperationResult<PaymentTransactionDto>.NotFoundResult(
                "Pending checkout transaction not found.");
        }

        transaction.Status = BillingStatus.Canceled;

        var account = await billingRepository.GetBillingAccountAsync(
            membership.InstitutionId,
            asTracking: true,
            cancellationToken);
        if (account is not null && account.Status == BillingStatus.Pending)
        {
            account.Status = BillingStatus.Canceled;
        }

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
}
