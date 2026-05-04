using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class SpellingTestEndpoints : ICarterModule
{
    private const double Version = 1.1;
    private const string Tag = "Spelling Tests";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/classrooms/{classroomId:int}/spelling-tests",
            async (
                int classroomId,
                [FromBody] CreateSpellingTestRequest request,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var teacherId = int.Parse(user.Identity.GetUserId());
                    var result = await service.CreateAsync(classroomId, request, teacherId, user.IsInRole("admin"), cancellationToken);
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
            }), Version, "CreateSpellingTest", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

        app.MapEndpoint(builder => builder.MapGet(
            "/api/v{version:apiVersion}/classrooms/{classroomId:int}/spelling-tests",
            async (
                int classroomId,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var teacherId = int.Parse(user.Identity.GetUserId());
                    return Results.Ok(await service.GetForTeacherAsync(classroomId, teacherId, user.IsInRole("admin"), cancellationToken));
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "GetClassroomSpellingTests", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

        app.MapEndpoint(builder => builder.MapGet(
            "/api/v{version:apiVersion}/spelling-tests/{testId:int}",
            async (
                int testId,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var teacherId = int.Parse(user.Identity.GetUserId());
                    return Results.Ok(await service.GetTeacherDetailAsync(testId, teacherId, user.IsInRole("admin"), cancellationToken));
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
            }), Version, "GetSpellingTest", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

        app.MapEndpoint(builder => builder.MapGet(
            "/api/v{version:apiVersion}/spelling-tests/{testId:int}/results",
            async (
                int testId,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var teacherId = int.Parse(user.Identity.GetUserId());
                    return Results.Ok(await service.GetTeacherResultsAsync(testId, teacherId, user.IsInRole("admin"), cancellationToken));
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
            }), Version, "GetSpellingTestResults", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

        app.MapEndpoint(builder => builder.MapPatch(
            "/api/v{version:apiVersion}/spelling-tests/{testId:int}",
            async (
                int testId,
                [FromBody] UpdateSpellingTestRequest request,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var teacherId = int.Parse(user.Identity.GetUserId());
                    return Results.Ok(await service.UpdateAsync(testId, request, teacherId, user.IsInRole("admin"), cancellationToken));
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "UpdateSpellingTest", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

        app.MapEndpoint(builder => builder.MapDelete(
            "/api/v{version:apiVersion}/spelling-tests/{testId:int}",
            async (
                int testId,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var teacherId = int.Parse(user.Identity.GetUserId());
                    return Results.Ok(await service.ArchiveAsync(testId, teacherId, user.IsInRole("admin"), cancellationToken));
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "ArchiveSpellingTest", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

        app.MapEndpoint(builder => builder.MapGet(
            "/api/v{version:apiVersion}/student/spelling-tests/active",
            async (
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                var studentId = int.Parse(user.Identity.GetUserId());
                return Results.Ok(await service.GetActiveForStudentAsync(studentId, cancellationToken));
            }), Version, "GetStudentActiveSpellingTests", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapGet(
            "/api/v{version:apiVersion}/student/spelling-tests/{testId:int}",
            async (
                int testId,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var studentId = int.Parse(user.Identity.GetUserId());
                    return Results.Ok(await service.GetStudentDetailAsync(testId, studentId, cancellationToken));
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
            }), Version, "GetStudentSpellingTest", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/student/spelling-tests/{testId:int}/start",
            async (
                int testId,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var studentId = int.Parse(user.Identity.GetUserId());
                    return Results.Ok(await service.StartAsync(testId, studentId, cancellationToken));
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "StartStudentSpellingTest", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/student/spelling-tests/{testId:int}/resume",
            async (
                int testId,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var studentId = int.Parse(user.Identity.GetUserId());
                    return Results.Ok(await service.ResumeAsync(testId, studentId, cancellationToken));
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "ResumeStudentSpellingTest", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/student/spelling-tests/{testId:int}/pause",
            async (
                int testId,
                [FromBody] PauseSpellingTestRequest request,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var studentId = int.Parse(user.Identity.GetUserId());
                    return Results.Ok(await service.PauseAsync(testId, studentId, request, cancellationToken));
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "PauseStudentSpellingTest", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/student/spelling-tests/{testId:int}/submit",
            async (
                int testId,
                [FromBody] SubmitSpellingTestRequest request,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var studentId = int.Parse(user.Identity.GetUserId());
                    return Results.Ok(await service.SubmitAsync(testId, studentId, request, cancellationToken));
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "SubmitStudentSpellingTest", Tag)
            .RequireAuthorization();

        app.MapEndpoint(builder => builder.MapPost(
            "/api/v{version:apiVersion}/student/spelling-tests/{testId:int}/dismiss-modal",
            async (
                int testId,
                ClaimsPrincipal user,
                ISpellingTestService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var studentId = int.Parse(user.Identity.GetUserId());
                    return Results.Ok(await service.DismissModalAsync(testId, studentId, cancellationToken));
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "DismissStudentSpellingTestModal", Tag)
            .RequireAuthorization();
    }
}
