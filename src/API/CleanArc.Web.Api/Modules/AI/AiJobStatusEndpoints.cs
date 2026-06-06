using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Carter;
using CleanArc.Infrastructure.Persistence;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Web.Api.Endpoints;

public sealed class AiJobStatusEndpoints : ICarterModule
{
    private const string RoutePrefix = "/api/v{version:apiVersion}/ai/jobs";
    private const double Version = 1.1;
    private const string Tag = "AI Job Status";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapEndpoint(builder => builder.MapGet(
            $"{RoutePrefix}/{{auditLogId:int}}",
            async (
                int auditLogId,
                ClaimsPrincipal user,
                ApplicationDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var userId = int.Parse(user.Identity!.GetUserId());

                var log = await dbContext.AiAuditLogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == auditLogId, cancellationToken);

                if (log is null)
                {
                    return Results.NotFound(new { message = "Job not found." });
                }

                // Verify authorization - only creator of job or admin
                if (log.RelatedUserId != userId)
                {
                    return Results.Json(new { message = "Unauthorized access to this job." }, statusCode: StatusCodes.Status401Unauthorized);
                }

                object? resultPayload = null;

                if (log.ValidationStatus == "VALID")
                {
                    if (log.UseCase == "WEEKLY_REPORT_GENERATION")
                    {
                        resultPayload = new { reportMarkdown = log.RawOutputJson };
                    }
                    else if (!string.IsNullOrWhiteSpace(log.ParsedOutputJson) && log.ParsedOutputJson != "null")
                    {
                        try
                        {
                            resultPayload = JsonSerializer.Deserialize<object>(log.ParsedOutputJson);
                        }
                        catch
                        {
                            resultPayload = log.ParsedOutputJson;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(log.RawOutputJson) && log.RawOutputJson != "null")
                    {
                        try
                        {
                            resultPayload = JsonSerializer.Deserialize<object>(log.RawOutputJson);
                        }
                        catch
                        {
                            resultPayload = log.RawOutputJson;
                        }
                    }
                }

                var errors = string.IsNullOrWhiteSpace(log.ValidationErrorsJson)
                    ? Array.Empty<string>()
                    : JsonSerializer.Deserialize<string[]>(log.ValidationErrorsJson);

                return Results.Ok(new
                {
                    auditLogId = log.Id,
                    useCase = log.UseCase,
                    status = log.ValidationStatus,
                    errors = errors,
                    result = resultPayload
                });
            }), Version, "GetAiJobStatus", Tag)
            .RequireAuthorization();
    }
}
