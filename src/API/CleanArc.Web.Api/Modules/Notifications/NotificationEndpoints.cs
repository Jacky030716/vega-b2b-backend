using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CleanArc.Web.Api.Endpoints;

public record DeviceTokenRequest(string Token, string? Platform);

public class NotificationEndpoints : ICarterModule
{
    private const double Version = 1.1;
    private const string Tag = "Notifications";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/notifications/device-token",
            async (
                [FromBody] DeviceTokenRequest request,
                ClaimsPrincipal user,
                ISrsNotificationService srsNotificationService,
                CancellationToken cancellationToken) =>
            {
                var studentId = int.Parse(user.Identity.GetUserId());
                await srsNotificationService.RegisterPushTokenAsync(studentId, request.Token, cancellationToken);
                return Results.Ok(new { success = true });
            }), Version, "RegisterDeviceToken", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapGet(
            "/api/v{version:apiVersion}/notifications",
            () => Results.Ok(Array.Empty<object>())), Version, "GetNotifications", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapPatch(
            "/api/v{version:apiVersion}/notifications/{alertId}/read",
            () => Results.Ok(new { success = true })), Version, "ReadNotification", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapDelete(
            "/api/v{version:apiVersion}/notifications/{alertId}",
            () => Results.Ok(new { success = true })), Version, "DeleteNotification", Tag)
            .RequireAuthorization();
    }
}
