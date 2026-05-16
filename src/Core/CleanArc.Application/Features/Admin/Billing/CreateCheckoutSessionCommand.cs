using CleanArc.Application.Contracts.Infrastructure.Billing;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Billing;

public sealed record CreateCheckoutSessionCommand(
    int UserId,
    string SuccessUrl,
    string CancelUrl,
    decimal Amount = 299m,
    string Currency = "MYR",
    string PlanId = "standard-demo") : IRequest<OperationResult<CheckoutSessionDto>>;

internal sealed class CreateCheckoutSessionCommandHandler(IBillingPaymentService billingPaymentService)
    : IRequestHandler<CreateCheckoutSessionCommand, OperationResult<CheckoutSessionDto>>
{
    public ValueTask<OperationResult<CheckoutSessionDto>> Handle(
        CreateCheckoutSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SuccessUrl) || string.IsNullOrWhiteSpace(request.CancelUrl))
        {
            return ValueTask.FromResult(
                OperationResult<CheckoutSessionDto>.FailureResult("Checkout return URLs are required."));
        }

        return new ValueTask<OperationResult<CheckoutSessionDto>>(
            billingPaymentService.CreateCheckoutSessionAsync(
                request.UserId,
                request.SuccessUrl.Trim(),
                request.CancelUrl.Trim(),
                request.Amount <= 0 ? 299m : request.Amount,
                string.IsNullOrWhiteSpace(request.Currency) ? "MYR" : request.Currency.Trim().ToUpperInvariant(),
                string.IsNullOrWhiteSpace(request.PlanId) ? "standard-demo" : request.PlanId.Trim(),
                cancellationToken));
    }
}
