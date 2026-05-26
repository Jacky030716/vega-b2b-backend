using CleanArc.Application.Contracts.Infrastructure.Billing;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Admin.Billing;

public sealed record CreateCheckoutSessionCommand(
    int UserId,
    string SuccessUrl,
    string CancelUrl,
    string PlanId = SubscriptionPlanCatalog.DefaultPlanId) : IRequest<OperationResult<CheckoutSessionDto>>;

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

        var plan = SubscriptionPlanCatalog.Find(request.PlanId);
        if (plan is null)
        {
            return ValueTask.FromResult(
                OperationResult<CheckoutSessionDto>.FailureResult("The selected subscription plan is not available."));
        }

        return new ValueTask<OperationResult<CheckoutSessionDto>>(
            billingPaymentService.CreateCheckoutSessionAsync(
                request.UserId,
                request.SuccessUrl.Trim(),
                request.CancelUrl.Trim(),
                plan,
                cancellationToken));
    }
}
