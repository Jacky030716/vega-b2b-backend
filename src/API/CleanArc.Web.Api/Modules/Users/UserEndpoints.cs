using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Features.Users.Commands.Create;
using CleanArc.Application.Features.Users.Commands.ForgotPassword;
using CleanArc.Application.Features.Users.Commands.RefreshUserTokenCommand;
using CleanArc.Application.Features.Users.Commands.RequestLogout;
using CleanArc.Application.Features.Users.Commands.ResetStudentNotificationPreferences;
using CleanArc.Application.Features.Users.Commands.UpdateStudentNotificationPreferences;
using CleanArc.Application.Features.Users.Commands.UpdateUserProfile;
using CleanArc.Application.Features.Users.Queries.GenerateUserToken;
using CleanArc.Application.Features.Users.Queries.GetStudentNotificationPreferences;
using CleanArc.Application.Features.Users.Queries.PasswordLogin;
using CleanArc.Application.Features.Users.Queries.GetUserProfile;
using CleanArc.Application.Features.Users.Queries.StudentVisualChallenge;
using CleanArc.Application.Features.Users.Queries.StudentVisualLogin;
using CleanArc.Application.Features.Users.Queries.TokenRequest;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class UserEndpoints : ICarterModule
{
    private readonly string _routePrefix = "/api/v{version:apiVersion}/Users/";
    private readonly double _version = 1.1;
    private readonly string _tag = "User";
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapEndpoint(
            builder => builder.MapPost($"{_routePrefix}Register", async (UserCreateCommand model, ISender sender) =>
        {
            var result = await sender.Send(model);
            return result.ToEndpointResult();
        }), _version, "Register", _tag);


        app.MapEndpoint(
            builder => builder.MapPost($"{_routePrefix}TokenRequest", async (UserTokenRequestQuery model, ISender sender) =>
            {
                var result = await sender.Send(model);
                return result.ToEndpointResult();
            }), _version, "TokenRequest", _tag);


        app.MapEndpoint(
            builder => builder.MapPost($"{_routePrefix}LoginConfirmation", async (GenerateUserTokenQuery model, ISender sender) =>
            {
                var result = await sender.Send(model);
                return result.ToEndpointResult();
            }), _version, "LoginConfirmation", _tag);

        app.MapEndpoint(
            builder => builder.MapPost($"{_routePrefix}Login", async ([FromBody] UserPasswordLoginQuery model, ISender sender) =>
            {
                var result = await sender.Send(model);
                return result.ToEndpointResult();
            }), _version, "Login", _tag);

        app.MapEndpoint(
            builder => builder.MapPost($"{_routePrefix}Student/Challenge", async ([FromBody] StudentVisualChallengeQuery model, ISender sender) =>
            {
                var result = await sender.Send(model);
                return result.ToEndpointResult();
            }), _version, "StudentChallenge", _tag);

        app.MapEndpoint(
            builder => builder.MapPost($"{_routePrefix}Student/Login", async ([FromBody] StudentVisualLoginQuery model, ISender sender) =>
            {
                var result = await sender.Send(model);
                return result.ToEndpointResult();
            }), _version, "StudentVisualLogin", _tag);

        app.MapEndpoint(
            builder => builder.MapGet($"{_routePrefix}Student/VisualIcons", async (ISender sender) =>
            {
                var result = await sender.Send(new CleanArc.Application.Features.Users.Queries.StudentVisualIcons.GetStudentVisualIconsQuery());
                return result.ToEndpointResult();
            }), _version, "GetStudentVisualIcons", _tag);

        app.MapEndpoint(
            builder => builder.MapGet($"{_routePrefix}RefreshSignIn", async (Guid userRefreshToken, ISender sender) =>
            {

                var result = await sender.Send(new RefreshUserTokenCommand(userRefreshToken));
                return result.ToEndpointResult();
            }), _version, "RefreshSignIn", _tag);

        app.MapEndpoint(
            builder => builder.MapGet($"{_routePrefix}Logout", async (ClaimsPrincipal user, ISender sender) =>
            {

                var result = await sender.Send(new RequestLogoutCommand(int.Parse(user.Identity.GetUserId())));
                return result.ToEndpointResult();
            }), _version, "Logout", _tag)
            .RequireAuthorization();

        app.MapEndpoint(
            builder => builder.MapGet($"{_routePrefix}Profile", async (
                ClaimsPrincipal user, 
                ISender sender,
                ISrsNotificationService srsNotificationService,
                CancellationToken cancellationToken) =>
            {
                var userId = int.Parse(user.Identity.GetUserId());
                
                // Fire-and-forget background notification check on first profile load of the day
                _ = Task.Run(async () => {
                    try
                    {
                        await srsNotificationService.SendNotificationIfOverdueAsync(userId, cancellationToken);
                    }
                    catch
                    {
                        // Avoid crashing main execution thread
                    }
                }, cancellationToken);

                var result = await sender.Send(new GetUserProfileQuery(userId));
                return result.ToEndpointResult();
            }), _version, "GetProfile", _tag)
            .RequireAuthorization();

        app.MapEndpoint(
            builder => builder.MapPut($"{_routePrefix}Profile", async ([FromBody] UpdateUserProfileRequest request, ClaimsPrincipal user, ISender sender) =>
            {
                var result = await sender.Send(new UpdateUserProfileCommand(int.Parse(user.Identity.GetUserId()), request));
                return result.ToEndpointResult();
            }), _version, "UpdateProfile", _tag)
            .RequireAuthorization();

        app.MapEndpoint(
            builder => builder.MapGet($"{_routePrefix}NotificationPreferences", async (
                ClaimsPrincipal user,
                ISender sender) =>
            {
                var studentId = int.Parse(user.Identity.GetUserId());
                var result = await sender.Send(new GetStudentNotificationPreferencesQuery(studentId));
                return result.ToEndpointResult();
            }), _version, "GetNotificationPreferences", _tag)
            .RequireAuthorization(builder => builder.RequireRole("student"));

        app.MapEndpoint(
            builder => builder.MapPut($"{_routePrefix}NotificationPreferences", async (
                [FromBody] UpdateStudentNotificationPreferencesRequest request,
                ClaimsPrincipal user,
                ISender sender) =>
            {
                var studentId = int.Parse(user.Identity.GetUserId());
                var result = await sender.Send(new UpdateStudentNotificationPreferencesCommand(
                    studentId,
                    request.InAppNotificationsEnabled,
                    request.PracticeRemindersEnabled,
                    request.StreakRemindersEnabled,
                    request.AchievementAlertsEnabled,
                    request.WeeklyReportsEnabled,
                    request.ReminderTimeLocal,
                    request.QuietHoursStartLocal,
                    request.QuietHoursEndLocal,
                    request.NotificationTimezone));
                return result.ToEndpointResult();
            }), _version, "UpdateNotificationPreferences", _tag)
            .RequireAuthorization(builder => builder.RequireRole("student"));

        app.MapEndpoint(
            builder => builder.MapPost($"{_routePrefix}NotificationPreferences/Reset", async (
                ClaimsPrincipal user,
                ISender sender) =>
            {
                var studentId = int.Parse(user.Identity.GetUserId());
                var result = await sender.Send(new ResetStudentNotificationPreferencesCommand(studentId));
                return result.ToEndpointResult();
            }), _version, "ResetNotificationPreferences", _tag)
            .RequireAuthorization(builder => builder.RequireRole("student"));

        app.MapEndpoint(
            builder => builder.MapPost($"{_routePrefix}ForgotPassword", async ([FromBody] RequestPasswordResetCommand model, ISender sender) =>
            {
                var result = await sender.Send(model);
                return result.ToEndpointResult();
            }), _version, "ForgotPassword", _tag);

        app.MapEndpoint(
            builder => builder.MapPost($"{_routePrefix}ResetPassword", async ([FromBody] ResetPasswordCommand model, ISender sender) =>
            {
                var result = await sender.Send(model);
                return result.ToEndpointResult();
            }), _version, "ResetPassword", _tag);
    }
}

public record UpdateStudentNotificationPreferencesRequest(
    bool InAppNotificationsEnabled,
    bool PracticeRemindersEnabled,
    bool StreakRemindersEnabled,
    bool AchievementAlertsEnabled,
    bool WeeklyReportsEnabled,
    string ReminderTimeLocal,
    string QuietHoursStartLocal,
    string QuietHoursEndLocal,
    string NotificationTimezone);
