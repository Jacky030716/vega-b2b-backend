using System.Security.Claims;
using System.Text.Json;
using Carter;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Microsoft.AspNetCore.Http;
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
                IUnitOfWork unitOfWork,
                IAiRateLimitService aiRateLimitService,
                IAiUsageService aiUsageService,
                IAiAuditService aiAuditService,
                CancellationToken cancellationToken) =>
            {
                var teacherId = int.Parse(user.Identity!.GetUserId());
                
                // 1. Verify classroom
                var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
                if (classroom is null)
                {
                    return Results.NotFound(new { message = "Classroom not found." });
                }

                if (classroom.TeacherId != teacherId)
                {
                    return Results.Json(new { message = "You do not manage this classroom." }, statusCode: StatusCodes.Status401Unauthorized);
                }

                // 2. Perform AI governance checks (Rate limit and Quota)
                var rateLimit = await aiRateLimitService.TryAcquireAsync(teacherId, AiFeatureTypes.WeeklyReportGeneration, cancellationToken);
                if (!rateLimit.Allowed)
                {
                    return Results.Json(
                        new { message = "Too many AI requests. Please try again later.", retryAfterSeconds = rateLimit.RetryAfterSeconds },
                        statusCode: StatusCodes.Status429TooManyRequests);
                }

                var quota = await aiUsageService.GetRemainingQuotaAsync(teacherId, AiFeatureTypes.WeeklyReportGeneration, cancellationToken);
                if (quota.Remaining <= 0)
                {
                    return Results.BadRequest(new { message = "Your AI quota is exhausted for this month." });
                }

                // 3. Start Audit Log
                var auditInputContext = new
                {
                    request.ClassroomId,
                    classroomName = classroom.Name,
                    classroomSubject = classroom.Subject,
                    classroomYearLevel = classroom.YearLevel
                };

                var auditLogId = await aiAuditService.StartAsync(
                    new AiAuditStartRequest(
                        UseCase: "WEEKLY_REPORT_GENERATION",
                        Provider: "GEMINI",
                        ModelName: null,
                        PromptVersion: "v1",
                        InputPayloadJson: JsonSerializer.Serialize(auditInputContext),
                        RelatedUserId: teacherId,
                        RelatedClassroomId: request.ClassroomId),
                    cancellationToken);

                // 4. Enqueue Hangfire background job
                Hangfire.BackgroundJob.Enqueue<IBackgroundJobExecutor>(x =>
                    x.ExecuteWeeklyReportJobAsync(auditLogId, request.ClassroomId, teacherId));

                return Results.Accepted($"/api/v1.1/ai/jobs/{auditLogId}", new { auditLogId, status = "PENDING" });
            }), Version, "GenerateWeeklyReport", Tag)
            .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));
    }
}


public sealed record WeeklyReportRequest(int ClassroomId);
