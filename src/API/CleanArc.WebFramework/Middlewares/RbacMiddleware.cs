using System.Security.Claims;
using CleanArc.Application.Models.ApiResult;
using CleanArc.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace CleanArc.WebFramework.Middlewares;

public class RbacMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RbacMiddleware> _logger;

    public RbacMiddleware(RequestDelegate next, ILogger<RbacMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        var authorizeData = endpoint.Metadata.GetMetadata<IAuthorizeData>();
        var allowAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>();

        if (authorizeData == null || allowAnonymous != null)
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var routeEndpoint = endpoint as RouteEndpoint;
        var routeTemplate = routeEndpoint?.RoutePattern.RawText ?? "";
        var method = context.Request.Method;

        var isStudentEp = IsStudentEndpoint(method, routeTemplate, context);
        var isEducatorEp = IsEducatorEndpoint(method, routeTemplate, context);

        if (isStudentEp || isEducatorEp)
        {
            var isStudent = context.User.IsInRole("student");
            var isTeacher = context.User.IsInRole("teacher") || context.User.IsInRole("educator");
            var isAdmin = context.User.IsInRole("admin") || context.User.IsInRole("Admin") || context.User.IsInRole("InstitutionAdmin");

            if (isStudentEp)
            {
                // Restrict access strictly to student role.
                // Deny if they don't have "student" role, or if they have "teacher", "educator", or "admin" roles.
                if (!isStudent || isTeacher || isAdmin)
                {
                    _logger.LogWarning($"User '{context.User.Identity.Name}' denied access to student endpoint '{routeTemplate}' due to role mismatch.");
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new ApiResult(false, ApiResultStatusCode.Forbidden, "Forbidden"));
                    return;
                }
            }
            else if (isEducatorEp)
            {
                // Restrict access strictly to educator/teacher/admin roles.
                // Deny if they have "student" role, or if they don't have teacher/educator/admin roles.
                if (isStudent || (!isTeacher && !isAdmin))
                {
                    _logger.LogWarning($"User '{context.User.Identity.Name}' denied access to educator endpoint '{routeTemplate}' due to role mismatch.");
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new ApiResult(false, ApiResultStatusCode.Forbidden, "Forbidden"));
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool IsStudentEndpoint(string method, string routeTemplate, HttpContext context)
    {
        routeTemplate = routeTemplate.ToLowerInvariant();

        if (routeTemplate.Contains("/student/") || routeTemplate.Contains("/student"))
            return true;
        if (routeTemplate.Contains("/badges/") || routeTemplate.Contains("/badges"))
            return true;
        if (routeTemplate.Contains("/activity/") || routeTemplate.Contains("/activity"))
            return true;
        if (routeTemplate.Contains("/progression/") || routeTemplate.Contains("/progression"))
            return true;
        if (routeTemplate.Contains("/shop/") || routeTemplate.Contains("/shop"))
            return true;
        if (routeTemplate.Contains("/stickers/") || routeTemplate.Contains("/stickers"))
            return true;
        if (routeTemplate.Contains("/streaks/") || routeTemplate.Contains("/streaks"))
            return true;
        if (routeTemplate.Contains("/attempts/") || routeTemplate.Contains("/attempts"))
            return true;
        if (routeTemplate.Contains("students/{studentid:int}/mastery") || routeTemplate.Contains("students/{studentid}/mastery"))
            return true;
        if (routeTemplate.Contains("students/{studentid:int}/weakness-summary") || routeTemplate.Contains("students/{studentid}/weakness-summary"))
            return true;
        if (routeTemplate.Contains("students/{studentid:int}/recommended-next-challenges") || routeTemplate.Contains("students/{studentid}/recommended-next-challenges"))
            return true;
        if (routeTemplate.Contains("adaptive/recommendations/student"))
            return true;
        if (routeTemplate.Contains("adaptive/challenges/{id}"))
            return true;
        if (routeTemplate.Contains("ai/game/smart-feedback"))
            return true;

        if (routeTemplate.Contains("classrooms/{classroomid}/challenges") || routeTemplate.Contains("classrooms/{classroomid:int}/challenges"))
        {
            var view = context.Request.Query["view"].ToString();
            if (string.Equals(view, "student", StringComparison.OrdinalIgnoreCase))
                return true; // Student adventure map
        }

        if (routeTemplate.Contains("games/{gamekey}/challenges"))
        {
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                return true; // Student adventure map
        }

        if (routeTemplate.Contains("games/attempts"))
            return true;

        if (routeTemplate.Contains("games/{gamekey}/challenges/{challengeid:int}/attempts"))
            return true;

        // General user profile is considered a student endpoint
        if (routeTemplate.Contains("users/profile") || routeTemplate.Contains("user/profile"))
            return true;

        return false;
    }

    private static bool IsEducatorEndpoint(string method, string routeTemplate, HttpContext context)
    {
        routeTemplate = routeTemplate.ToLowerInvariant();

        if (routeTemplate.Contains("/educator/") || routeTemplate.Contains("/educator"))
            return true;
        if (routeTemplate.Contains("/teachers/") || routeTemplate.Contains("/teachers") || routeTemplate.Contains("/teacher/") || routeTemplate.Contains("/teacher"))
            return true;
        if (routeTemplate.Contains("/admin/") || routeTemplate.Contains("/admin") || routeTemplate.Contains("/adminmanager/") || routeTemplate.Contains("/adminmanager"))
            return true;
        if (routeTemplate.Contains("/challenges/board") || routeTemplate.Contains("/challenges/recommended") || routeTemplate.Contains("challenges/{challengeid:int}/lifecycle"))
            return true;
        if (routeTemplate.Contains("/modules/{moduleid:int}/challenges") || routeTemplate.Contains("/custom-modules/"))
            return true;
        if (routeTemplate.Contains("/challenges/{challengeid:int}"))
        {
            if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        if (routeTemplate.Contains("classrooms/{classroomid}/challenges") || routeTemplate.Contains("classrooms/{classroomid:int}/challenges"))
        {
            var view = context.Request.Query["view"].ToString();
            if (!string.Equals(view, "student", StringComparison.OrdinalIgnoreCase))
                return true; // Educator classroom challenges view
        }

        if (routeTemplate.Contains("games/{gamekey}/challenges"))
        {
            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                return true; // Create challenge
        }
        if (routeTemplate.Contains("games/{gamekey}/challenges/ai-draft"))
            return true;
        if (routeTemplate.Contains("ai/classroom-thumbnails") || routeTemplate.Contains("ai/weekly-report"))
            return true;

        if (routeTemplate.Contains("syllabus/modules"))
        {
            if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                return true; // POST/PUT/DELETE syllabus modules
        }
        if (routeTemplate.Contains("syllabus/modules/{id:int}"))
        {
            if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                return true; // POST/PUT/DELETE
        }

        return false;
    }
}

public static class RbacMiddlewareExtensions
{
    public static IApplicationBuilder UseRbac(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RbacMiddleware>();
    }
}
