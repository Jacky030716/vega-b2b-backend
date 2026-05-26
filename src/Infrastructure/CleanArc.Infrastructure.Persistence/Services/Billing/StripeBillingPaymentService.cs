#nullable enable

using CleanArc.Application.Contracts.Infrastructure.Billing;
using CleanArc.Application.Features.Admin.Billing;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace CleanArc.Infrastructure.Persistence.Services.Billing;

internal sealed class StripeBillingPaymentService(
    ApplicationDbContext dbContext,
    IConfiguration configuration)
    : IBillingPaymentService
{
    private const string Provider = "stripe";

    public async Task<OperationResult<CheckoutSessionDto>> CreateCheckoutSessionAsync(
        int userId,
        string successUrl,
        string cancelUrl,
        SubscriptionPlanDto plan,
        CancellationToken cancellationToken = default)
    {
        var secretKey = configuration["STRIPE_SECRET_KEY"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return OperationResult<CheckoutSessionDto>.FailureResult(
                "Stripe test mode is not configured on the server.");
        }

        StripeConfiguration.ApiKey = secretKey;

        var institution = await ResolveInstitutionAsync(userId, cancellationToken);
        if (institution is null)
            return OperationResult<CheckoutSessionDto>.NotFoundResult("Institution not found.");

        var account = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(x => x.InstitutionId == institution.Id, cancellationToken);
        if (account is null)
        {
            account = new BillingAccount
            {
                InstitutionId = institution.Id,
                PlanId = plan.Id,
                ActivePlanId = SubscriptionPlanCatalog.ForExistingSubscription(null, institution.SubscriptionTier).Id,
                Status = BillingStatus.None,
            };
            dbContext.BillingAccounts.Add(account);
        }

        var customerId = account.StripeCustomerId;
        if (string.IsNullOrWhiteSpace(customerId))
        {
            var customer = await new CustomerService().CreateAsync(
                new CustomerCreateOptions
                {
                    Name = institution.Name,
                    Metadata = new Dictionary<string, string>
                    {
                        ["institutionId"] = institution.Id.ToString(),
                    },
                },
                cancellationToken: cancellationToken);

            customerId = customer.Id;
            account.StripeCustomerId = customerId;
            institution.StripeCustomerId = customerId;
        }

        var session = await new SessionService().CreateAsync(
            new SessionCreateOptions
            {
                Mode = "payment",
                Customer = customerId,
                PaymentMethodTypes = ["card", "fpx", "grabpay"],
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = plan.Currency.ToLowerInvariant(),
                            UnitAmount = decimal.ToInt64(plan.Amount),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Vega {plan.Name} subscription ({plan.BillingInterval})",
                            },
                        },
                    },
                ],
                Metadata = new Dictionary<string, string>
                {
                    ["institutionId"] = institution.Id.ToString(),
                    ["planId"] = plan.Id,
                    ["isDemo"] = "true",
                },
            },
            cancellationToken: cancellationToken);

        var transaction = new PaymentTransaction
        {
            InstitutionId = institution.Id,
            Provider = Provider,
            PaymentMethod = "card",
            PlanId = plan.Id,
            Amount = plan.Amount,
            Currency = plan.Currency,
            Status = BillingStatus.Pending,
            StripeCheckoutSessionId = session.Id,
            IsDemo = false,
        };

        account.PlanId = plan.Id;
        account.Status = BillingStatus.Pending;
        dbContext.PaymentTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResult<CheckoutSessionDto>.SuccessResult(new CheckoutSessionDto(
            session.Url,
            session.Id,
            BillingStatus.Pending));
    }

    public async Task<OperationResult<bool>> ProcessStripeWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        var webhookSecret = configuration["STRIPE_WEBHOOK_SECRET"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return OperationResult<bool>.FailureResult("Stripe webhook secret is not configured.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);
        }
        catch (StripeException)
        {
            return OperationResult<bool>.FailureResult("Invalid Stripe webhook signature.");
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                if (stripeEvent.Data.Object is Session completedSession)
                {
                    await UpdateSessionAsync(
                        completedSession.Id,
                        completedSession.PaymentIntentId,
                        BillingStatus.Succeeded,
                        cancellationToken);
                }
                break;
            case "checkout.session.async_payment_failed":
                if (stripeEvent.Data.Object is Session failedSession)
                {
                    await UpdateSessionAsync(
                        failedSession.Id,
                        failedSession.PaymentIntentId,
                        BillingStatus.Failed,
                        cancellationToken);
                }
                break;
            case "payment_intent.succeeded":
                if (stripeEvent.Data.Object is PaymentIntent succeededIntent)
                {
                    await UpdatePaymentIntentAsync(
                        succeededIntent.Id,
                        BillingStatus.Succeeded,
                        cancellationToken);
                }
                break;
            case "payment_intent.payment_failed":
                if (stripeEvent.Data.Object is PaymentIntent failedIntent)
                {
                    await UpdatePaymentIntentAsync(
                        failedIntent.Id,
                        BillingStatus.Failed,
                        cancellationToken);
                }
                break;
        }

        return OperationResult<bool>.SuccessResult(true);
    }

    private async Task<CleanArc.Domain.Entities.Institution.Institution?> ResolveInstitutionAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var membership = await dbContext.InstitutionUsers
            .Include(x => x.Institution)
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.JoinedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership?.Institution is not null)
            return membership.Institution;

        var userInstitutionId = await dbContext.Users
            .Where(x => x.Id == userId && x.InstitutionId.HasValue)
            .Select(x => x.InstitutionId)
            .FirstOrDefaultAsync(cancellationToken);

        return await dbContext.Institutions
            .FirstOrDefaultAsync(x => x.Id == (userInstitutionId ?? 1), cancellationToken);
    }

    private async Task UpdateSessionAsync(
        string checkoutSessionId,
        string? paymentIntentId,
        string status,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.PaymentTransactions
            .FirstOrDefaultAsync(x => x.StripeCheckoutSessionId == checkoutSessionId, cancellationToken);

        if (transaction is null)
            return;

        transaction.StripePaymentIntentId = string.IsNullOrWhiteSpace(paymentIntentId)
            ? transaction.StripePaymentIntentId
            : paymentIntentId;
        transaction.Status = status;

        var account = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(x => x.InstitutionId == transaction.InstitutionId, cancellationToken);
        if (account is not null)
        {
            account.Status = status;
            if (status == BillingStatus.Succeeded)
            {
                account.ActivePlanId = transaction.PlanId;
                await ApplySuccessfulPlanAsync(transaction.InstitutionId, transaction.PlanId, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdatePaymentIntentAsync(
        string paymentIntentId,
        string status,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.PaymentTransactions
            .FirstOrDefaultAsync(
                x => x.StripePaymentIntentId == paymentIntentId,
                cancellationToken);

        if (transaction is null)
            return;

        transaction.Status = status;

        var account = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(x => x.InstitutionId == transaction.InstitutionId, cancellationToken);
        if (account is not null)
        {
            account.Status = status;
            if (status == BillingStatus.Succeeded)
            {
                account.ActivePlanId = transaction.PlanId;
                await ApplySuccessfulPlanAsync(transaction.InstitutionId, transaction.PlanId, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplySuccessfulPlanAsync(
        int institutionId,
        string planId,
        CancellationToken cancellationToken)
    {
        var plan = SubscriptionPlanCatalog.Find(planId);
        if (plan is null)
            return;

        var institution = await dbContext.Institutions
            .FirstOrDefaultAsync(x => x.Id == institutionId, cancellationToken);
        if (institution is null)
            return;

        institution.SubscriptionTier = plan.Name;
        institution.RenewalDate = plan.BillingInterval == "annual"
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);
    }
}
