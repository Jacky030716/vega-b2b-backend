using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Notifications;
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
            async (
                ClaimsPrincipal user,
                INotificationInboxService notificationInboxService,
                CancellationToken cancellationToken) =>
            {
                var userId = int.Parse(user.Identity.GetUserId());
                var notifications = await notificationInboxService.GetLatestAsync(userId, 100, cancellationToken);
                return Results.Ok(notifications);
            }), Version, "GetNotifications", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapGet(
            "/api/v{version:apiVersion}/notifications/{alertId}",
            async (
                int alertId,
                ClaimsPrincipal user,
                INotificationInboxService notificationInboxService,
                CancellationToken cancellationToken) =>
            {
                var userId = int.Parse(user.Identity.GetUserId());
                var notification = await notificationInboxService.GetByIdAsync(alertId, userId, cancellationToken);
                return notification is null
                    ? Results.NotFound(new { message = "Notification not found." })
                    : Results.Ok(notification);
            }), Version, "GetNotificationById", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapPatch(
            "/api/v{version:apiVersion}/notifications/{alertId}/read",
            async (
                int alertId,
                ClaimsPrincipal user,
                INotificationInboxService notificationInboxService,
                CancellationToken cancellationToken) =>
            {
                var userId = int.Parse(user.Identity.GetUserId());
                var updated = await notificationInboxService.MarkAsReadAsync(alertId, userId, cancellationToken);
                return updated
                    ? Results.Ok(new { success = true })
                    : Results.NotFound(new { message = "Notification not found." });
            }), Version, "ReadNotification", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapDelete(
            "/api/v{version:apiVersion}/notifications/{alertId}",
            async (
                int alertId,
                ClaimsPrincipal user,
                INotificationInboxService notificationInboxService,
                CancellationToken cancellationToken) =>
            {
                var userId = int.Parse(user.Identity.GetUserId());
                var deleted = await notificationInboxService.DeleteAsync(alertId, userId, cancellationToken);
                return deleted
                    ? Results.Ok(new { success = true })
                    : Results.NotFound(new { message = "Notification not found." });
            }), Version, "DeleteNotification", Tag)
            .RequireAuthorization();
    }
}
