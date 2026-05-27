using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Classrooms.Queries;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public sealed class WeeklyReportAiEndpoints : ICarterModule
{
    private const string RoutePrefix = "/api/v{version:apiVersion}/ai/weekly-report";
    private const double Version = 1.1;
    private const string Tag = "AI Weekly Reports";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapEndpoint(builder => builder.MapPost(
            RoutePrefix,
            async (
                [FromBody] WeeklyReportRequest request,
                ClaimsPrincipal user,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var teacherId = int.Parse(user.Identity!.GetUserId());
                var query = new GenerateWeeklyReportQuery(request.ClassroomId, teacherId);
                var result = await sender.Send(query, cancellationToken);
                return result.ToEndpointResult();
            }), Version, "GenerateWeeklyReport", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));
    }
}

public sealed record WeeklyReportRequest(int ClassroomId);
