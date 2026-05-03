using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class RecoveryMissionEndpoints : ICarterModule
{
    private const double Version = 1.1;
    private const string Tag = "Recovery Missions";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/students/{studentId:int}/recovery-missions/preview",
            async (
                int studentId,
                [FromBody] RecoveryMissionPreviewRequest request,
                ClaimsPrincipal user,
                IRecoveryMissionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var teacherId = int.Parse(user.Identity.GetUserId());
                    var result = await service.PreviewAsync(studentId, request, teacherId, cancellationToken);
                    return Results.Ok(result);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "PreviewRecoveryMission", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/students/{studentId:int}/recovery-missions",
            async (
                int studentId,
                [FromBody] CreateRecoveryMissionRequest request,
                ClaimsPrincipal user,
                IRecoveryMissionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var teacherId = int.Parse(user.Identity.GetUserId());
                    var result = await service.CreateAsync(studentId, request, teacherId, cancellationToken);
                    return Results.Ok(result);
                }
                catch (DuplicateRecoveryMissionException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status409Conflict);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "CreateRecoveryMission", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

        app.MapEndpoint(builder => builder.MapGet(
            "/api/v{version:apiVersion}/students/{studentId:int}/recovery-missions",
            async (
                int studentId,
                ClaimsPrincipal user,
                IRecoveryMissionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var teacherId = int.Parse(user.Identity.GetUserId());
                    var result = await service.GetForTeacherAsync(studentId, teacherId, cancellationToken);
                    return Results.Ok(result);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                }
            }), Version, "GetTeacherRecoveryMissions", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

        app.MapEndpoint(builder => builder.MapGet(
            "/api/v{version:apiVersion}/student/recovery-missions/active",
            async (
                ClaimsPrincipal user,
                IRecoveryMissionService service,
                CancellationToken cancellationToken) =>
            {
                var studentId = int.Parse(user.Identity.GetUserId());
                var result = await service.GetActiveForStudentAsync(studentId, cancellationToken);
                return Results.Ok(result);
            }), Version, "GetStudentActiveRecoveryMissions", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/student/recovery-missions/{missionId:int}/start",
            async (
                int missionId,
                ClaimsPrincipal user,
                IRecoveryMissionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var studentId = int.Parse(user.Identity.GetUserId());
                    var result = await service.StartAsync(missionId, studentId, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
            }), Version, "StartRecoveryMission", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/student/recovery-missions/{missionId:int}/complete",
            async (
                int missionId,
                ClaimsPrincipal user,
                IRecoveryMissionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var studentId = int.Parse(user.Identity.GetUserId());
                    var result = await service.CompleteAsync(missionId, studentId, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "CompleteRecoveryMission", Tag)
            .RequireAuthorization();
    }
}
