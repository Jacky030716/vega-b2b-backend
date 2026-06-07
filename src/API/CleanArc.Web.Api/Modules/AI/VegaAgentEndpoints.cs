using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.AI;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public sealed class VegaAgentEndpoints : ICarterModule
{
    private const string RoutePrefix = "/api/v{version:apiVersion}/classrooms/{classroomId:int}/vega-agent";
    private const double Version = 1.1;
    private const string Tag = "Professor Vega Agent";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapEndpoint(builder => builder.MapGet(
            $"{RoutePrefix}/inbox",
            async (
                int classroomId,
                ClaimsPrincipal user,
                IVegaAgentService agentService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var teacherId = int.Parse(user.Identity!.GetUserId());
                    var inbox = await agentService.GetInboxRecommendationsAsync(classroomId, teacherId, cancellationToken);
                    return Results.Ok(inbox);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (System.Exception ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "GetVegaAgentInbox", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

        app.MapEndpoint(builder => builder.MapPost(
            $"{RoutePrefix}/approve",
            async (
                int classroomId,
                [FromBody] ApproveRecommendationRequest request,
                ClaimsPrincipal user,
                IVegaAgentService agentService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var teacherId = int.Parse(user.Identity!.GetUserId());
                    var success = await agentService.ApproveRecommendationAsync(classroomId, request.RecommendationId, teacherId, request.Payload, cancellationToken);
                    if (success)
                    {
                        return Results.Ok(new { success = true, message = "Recommendation approved and executed successfully." });
                    }
                    return Results.BadRequest(new { success = false, message = "Unable to approve or execute recommendation." });
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
                }
                catch (System.Exception ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            }), Version, "ApproveVegaAgentRecommendation", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));
    }
}

public sealed record ApproveRecommendationRequest(string RecommendationId, object? Payload);
