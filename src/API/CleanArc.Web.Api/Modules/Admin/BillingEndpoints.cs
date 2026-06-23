using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.Infrastructure.Billing;
using CleanArc.Application.Features.Admin.Billing;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public sealed class BillingEndpoints : ICarterModule
{
    private const double Version = 1.1;
    private const string Tag = "Admin Billing";
    private const string AdminRoles = "InstitutionAdmin,institutionadmin,Admin,admin";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapEndpoint(builder => builder.MapGet(
            "/api/v{version:apiVersion}/admin/billing/summary",
            async (ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
            {
                var userId = int.Parse(user.Identity!.GetUserId());
                var result = await sender.Send(new GetBillingSummaryQuery(userId), ct);
                return result.ToEndpointResult();
            }), Version, "GetAdminBillingSummary", Tag)
            .RequireAuthorization(b => b.RequireRole(AdminRoles.Split(',')));

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/admin/billing/checkout-session",
            async (
                [FromBody] CreateCheckoutSessionRequest request,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken ct) =>
            {
                var userId = int.Parse(user.Identity!.GetUserId());
                var result = await sender.Send(new CreateCheckoutSessionCommand(
                    userId,
                    request.SuccessUrl,
                    request.CancelUrl,
                    request.PlanId ?? SubscriptionPlanCatalog.DefaultPlanId), ct);
                return result.ToEndpointResult();
            }), Version, "CreateAdminBillingCheckoutSession", Tag)
            .RequireAuthorization(b => b.RequireRole(AdminRoles.Split(',')));

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/admin/billing/mock-wallet-payment",
            async (
                [FromBody] MockWalletPaymentRequest request,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken ct) =>
            {
                var userId = int.Parse(user.Identity!.GetUserId());
                var result = await sender.Send(new CreateMockWalletPaymentCommand(
                    userId,
                    request.PaymentMethod,
                    request.PlanId ?? SubscriptionPlanCatalog.DefaultPlanId), ct);
                return result.ToEndpointResult();
            }), Version, "CreateAdminMockWalletPayment", Tag)
            .RequireAuthorization(b => b.RequireRole(AdminRoles.Split(',')));

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/admin/billing/checkout-session/cancel",
            async (
                [FromBody] CancelCheckoutSessionRequest request,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken ct) =>
            {
                var userId = int.Parse(user.Identity!.GetUserId());
                var result = await sender.Send(new CancelCheckoutSessionCommand(
                    userId,
                    request.SessionId), ct);
                return result.ToEndpointResult();
            }), Version, "CancelAdminBillingCheckoutSession", Tag)
            .RequireAuthorization(b => b.RequireRole(AdminRoles.Split(',')));

        app.MapPost(
            "/api/stripe/webhook",
            async (HttpRequest request, IBillingPaymentService billingPaymentService, CancellationToken ct) =>
            {
                using var reader = new StreamReader(request.Body);
                var payload = await reader.ReadToEndAsync(ct);
                var signature = request.Headers["Stripe-Signature"].ToString();
                var result = await billingPaymentService.ProcessStripeWebhookAsync(payload, signature, ct);
                return result.ToEndpointResult();
            })
            .WithName("StripeWebhook")
            .WithTags(Tag)
            .AllowAnonymous();
    }

    public sealed record CreateCheckoutSessionRequest(
        string SuccessUrl,
        string CancelUrl,
        string? PlanId);

    public sealed record MockWalletPaymentRequest(
        string PaymentMethod,
        string? PlanId);

    public sealed record CancelCheckoutSessionRequest(string SessionId);
}
