using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.Application.Contracts.Infrastructure.Billing;
using Mediator;

namespace CleanArc.Application.Features.Admin.Billing;

public sealed record ResumeCheckoutSessionCommand(
    int UserId,
    string SessionId) : IRequest<OperationResult<CheckoutSessionDto>>;

internal sealed class ResumeCheckoutSessionCommandHandler(
    IUnitOfWork unitOfWork,
    IBillingRepository billingRepository,
    IBillingPaymentService billingPaymentService)
    : IRequestHandler<ResumeCheckoutSessionCommand, OperationResult<CheckoutSessionDto>>
{
    public async ValueTask<OperationResult<CheckoutSessionDto>> Handle(
        ResumeCheckoutSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return OperationResult<CheckoutSessionDto>.FailureResult(
                "Checkout session id is required.");
        }

        var membership = await unitOfWork.InstitutionRepository.GetPrimaryInstitutionForUserAsync(
            request.UserId,
            cancellationToken);

        if (membership is null)
        {
            return OperationResult<CheckoutSessionDto>.ForbiddenResult(
                "Unable to resolve institution membership for this billing action.");
        }

        var transaction = await billingRepository.GetPendingCheckoutTransactionAsync(
            membership.InstitutionId,
            request.SessionId.Trim(),
            cancellationToken);

        if (transaction is null)
        {
            return OperationResult<CheckoutSessionDto>.NotFoundResult(
                "Pending checkout transaction not found.");
        }

        // ponytail: resume the exact same Stripe Checkout page URL to avoid duplicate pending transactions
        var result = await billingPaymentService.GetCheckoutSessionUrlAsync(transaction.StripeCheckoutSessionId, cancellationToken);
        if (!result.IsSuccess)
        {
            return OperationResult<CheckoutSessionDto>.FailureResult(result.ErrorMessage ?? "Failed to retrieve Stripe session.");
        }

        return OperationResult<CheckoutSessionDto>.SuccessResult(new CheckoutSessionDto(
            result.Result,
            transaction.StripeCheckoutSessionId,
            transaction.Status));
    }
}
