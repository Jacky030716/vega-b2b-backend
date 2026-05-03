using System.Security.Claims;
using System.Text.Json;
using Carter;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Classrooms.Commands;
using CleanArc.Application.Features.Classrooms.Queries;
using CleanArc.Application.Features.Games.Queries;
using CleanArc.Web.Api.Contracts.Requests.Classrooms;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

#nullable enable

namespace CleanArc.Web.Api.Endpoints;

public class ClassroomEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Classrooms/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Classrooms";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    // Get student classrooms
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}student", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetStudentClassroomsQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetStudentClassrooms", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet("/api/v{version:apiVersion}/student/classrooms/{classroomId:int}/modules", async (
        int classroomId,
        ClaimsPrincipal user,
        IStudentModuleProgressionService service,
        CancellationToken cancellationToken) =>
    {
      try
      {
        var studentId = int.Parse(user.Identity.GetUserId());
        var result = await service.GetClassroomModulesAsync(classroomId, studentId, cancellationToken);
        return Results.Ok(result);
      }
      catch (UnauthorizedAccessException ex)
      {
        return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
      }
      catch (InvalidOperationException ex)
      {
        return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
      }
    }), _version, "GetStudentClassroomModules", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet("/api/v{version:apiVersion}/student/classrooms/{classroomId:int}/subjects", async (
        int classroomId,
        ClaimsPrincipal user,
        IStudentModuleProgressionService service,
        CancellationToken cancellationToken) =>
    {
      try
      {
        var studentId = int.Parse(user.Identity.GetUserId());
        var result = await service.GetClassroomSubjectsAsync(classroomId, studentId, cancellationToken);
        return Results.Ok(result);
      }
      catch (UnauthorizedAccessException ex)
      {
        return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
      }
      catch (InvalidOperationException ex)
      {
        return Results.NotFound(new { message = ex.Message });
      }
    }), _version, "GetStudentClassroomSubjects", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet("/api/v{version:apiVersion}/student/classrooms/{classroomId:int}/subjects/{subject}/modules", async (
        int classroomId,
        string subject,
        ClaimsPrincipal user,
        IStudentModuleProgressionService service,
        CancellationToken cancellationToken) =>
    {
      try
      {
        var studentId = int.Parse(user.Identity.GetUserId());
        var result = await service.GetClassroomModulesAsync(classroomId, studentId, subject, cancellationToken);
        return Results.Ok(result);
      }
      catch (UnauthorizedAccessException ex)
      {
        return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
      }
      catch (InvalidOperationException ex)
      {
        return Results.NotFound(new { message = ex.Message });
      }
    }), _version, "GetStudentSubjectModules", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet("/api/v{version:apiVersion}/student/modules/{moduleId:int}/progression", async (
        int moduleId,
        [FromQuery] int classroomId,
        ClaimsPrincipal user,
        IStudentModuleProgressionService service,
        CancellationToken cancellationToken) =>
    {
      try
      {
        var studentId = int.Parse(user.Identity.GetUserId());
        var result = await service.GetModuleProgressionAsync(moduleId, classroomId, studentId, cancellationToken);
        return Results.Ok(result);
      }
      catch (UnauthorizedAccessException ex)
      {
        return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
      }
      catch (InvalidOperationException ex)
      {
        return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
      }
    }), _version, "GetStudentModuleProgression", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet("/api/v{version:apiVersion}/student/modules/{moduleId:int}/adventure-map", async (
        int moduleId,
        [FromQuery] int classroomId,
        ClaimsPrincipal user,
        IStudentModuleProgressionService service,
        CancellationToken cancellationToken) =>
    {
      try
      {
        var studentId = int.Parse(user.Identity.GetUserId());
        var result = await service.GetModuleProgressionAsync(moduleId, classroomId, studentId, cancellationToken);
        return Results.Ok(result);
      }
      catch (UnauthorizedAccessException ex)
      {
        return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
      }
      catch (InvalidOperationException ex)
      {
        return Results.NotFound(new { message = ex.Message });
      }
    }), _version, "GetStudentModuleAdventureMap", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet("/api/v{version:apiVersion}/student/custom-challenges", async (
        [FromQuery] int classroomId,
        ClaimsPrincipal user,
        IStudentModuleProgressionService service,
        CancellationToken cancellationToken) =>
    {
      try
      {
        var studentId = int.Parse(user.Identity.GetUserId());
        var result = await service.GetCustomChallengesAsync(classroomId, studentId, cancellationToken);
        return Results.Ok(result);
      }
      catch (UnauthorizedAccessException ex)
      {
        return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
      }
      catch (InvalidOperationException ex)
      {
        return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
      }
    }), _version, "GetStudentCustomChallenges", _tag).RequireAuthorization();

    // Get teacher classrooms
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}teacher", async ([FromQuery] bool? includeDeleted, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var shouldIncludeDeleted = includeDeleted == true;
      if (shouldIncludeDeleted && !user.IsInRole("admin"))
      {
        return Results.Json(new Dictionary<string, List<string>>
        {
          { "GeneralError", new() { "Only admins can include archived classrooms." } }
        }, statusCode: StatusCodes.Status403Forbidden);
      }

      var result = await sender.Send(new GetTeacherClassroomsQuery(userId, shouldIncludeDeleted && user.IsInRole("admin")));
      return result.ToEndpointResult();
    }), _version, "GetTeacherClassrooms", _tag).RequireAuthorization();

    // Get classroom detail
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{classroomId}}", async (int classroomId, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetClassroomDetailQuery(classroomId, userId));
      return result.ToEndpointResult();
    }), _version, "GetClassroomDetail", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{classroomId:int}}/module-overview", async (
        int classroomId,
        ClaimsPrincipal user,
        IClassroomModuleManagementService service,
        CancellationToken cancellationToken) =>
    {
      try
      {
        var teacherId = int.Parse(user.Identity.GetUserId());
        var result = await service.GetModuleOverviewAsync(classroomId, teacherId, cancellationToken);
        return Results.Ok(result);
      }
      catch (UnauthorizedAccessException ex)
      {
        return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
      }
      catch (InvalidOperationException ex)
      {
        return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
      }
    }), _version, "GetClassroomModuleOverview", _tag)
    .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{classroomId:int}}/modules", async (
        int classroomId,
        ClaimsPrincipal user,
        IClassroomModuleManagementService service,
        CancellationToken cancellationToken) =>
    {
      try
      {
        var teacherId = int.Parse(user.Identity.GetUserId());
        var result = await service.GetClassroomModulesAsync(classroomId, teacherId, cancellationToken);
        return Results.Ok(result);
      }
      catch (UnauthorizedAccessException ex)
      {
        return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
      }
      catch (InvalidOperationException ex)
      {
        return Results.NotFound(new { message = ex.Message });
      }
    }), _version, "GetClassroomModules", _tag)
    .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{classroomId:int}}/custom-module", async (
        int classroomId,
        ClaimsPrincipal user,
        IClassroomModuleManagementService service,
        CancellationToken cancellationToken) =>
    {
      try
      {
        var teacherId = int.Parse(user.Identity.GetUserId());
        var result = await service.GetCustomModuleAsync(classroomId, teacherId, cancellationToken);
        return Results.Ok(result);
      }
      catch (UnauthorizedAccessException ex)
      {
        return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
      }
      catch (InvalidOperationException ex)
      {
        return Results.NotFound(new { message = ex.Message });
      }
    }), _version, "GetClassroomCustomModule", _tag)
    .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    // Get classroom members (crew)
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{classroomId}}/members", async (int classroomId, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetClassroomMembersQuery(classroomId, userId));
      return result.ToEndpointResult();
    }), _version, "GetClassroomMembers", _tag).RequireAuthorization();

    // Get educator-facing student diagnostics inside a classroom
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{classroomId}}/students/{{studentId}}/diagnostics", async (
        int classroomId,
        int studentId,
        ClaimsPrincipal user,
        ISender sender) =>
    {
      var teacherId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetClassroomStudentDiagnosticsQuery(classroomId, studentId, teacherId));
      return result.ToEndpointResult();
    }), _version, "GetClassroomStudentDiagnostics", _tag).RequireAuthorization();

    // Student adventure map compatibility alias (canonical path is /challenges?view=student)
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{classroomId}}/adventure-map", async (
        int classroomId,
        [FromQuery] string gameKey,
        ClaimsPrincipal user,
        ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetClassroomAdventureMapQuery(classroomId, userId, gameKey));
      return result.ToEndpointResult();
    }), _version, "GetClassroomAdventureMap", _tag).RequireAuthorization();

    // ── Classroom challenges ──────────────────────────────────────────────────

    // Get classroom challenges.
    // - Teacher/default view: completion counts per challenge.
    // - Student view (view=student): classroom-scoped adventure map nodes for the selected game.
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{classroomId}}/challenges", async (
        int classroomId,
        [FromQuery] string? gameKey,
        [FromQuery] string? view,
        ClaimsPrincipal user,
        ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var isStudentView = string.Equals(view, "student", StringComparison.OrdinalIgnoreCase);

      if (isStudentView)
      {
        if (string.IsNullOrWhiteSpace(gameKey))
        {
          return Results.BadRequest("The 'gameKey' query parameter is required when view=student.");
        }

        var studentResult = await sender.Send(new GetClassroomAdventureMapQuery(classroomId, userId, gameKey));
        return studentResult.ToEndpointResult();
      }

      var teacherResult = await sender.Send(new GetClassroomChallengesQuery(classroomId, userId));
      return teacherResult.ToEndpointResult();
    }), _version, "GetClassroomChallenges", _tag).RequireAuthorization();

    // Get the leaderboard for a specific challenge within a classroom
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{classroomId}}/challenges/{{challengeId:int}}/leaderboard", async (
        int classroomId,
        int challengeId,
        ISender sender) =>
    {
      var result = await sender.Send(new GetChallengeLeaderboardQuery(challengeId, classroomId));
      return result.ToEndpointResult();
    }), _version, "GetChallengeLeaderboard", _tag).RequireAuthorization();

    // ── Classroom management ──────────────────────────────────────────────────

    // Create classroom (teacher)
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}", async ([FromBody] CreateClassroomRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var thumbnailInfo = ParseThumbnail(request.Thumbnail);
      var result = await sender.Send(new CreateClassroomCommand(
        userId,
        request.Name,
        request.Description,
        request.Subject,
        request.Subjects,
        thumbnailInfo?.Url,
        request.YearLevel ?? 1,
        thumbnailInfo));
      return result.ToEndpointResult();
    }), _version, "CreateClassroom", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}{{classroomId:int}}/provision-modules", async (
        int classroomId,
        ClaimsPrincipal user,
        IUnitOfWork unitOfWork) =>
    {
      try
      {
        var userId = int.Parse(user.Identity.GetUserId());
        var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(classroomId, includeDeleted: false);
        if (classroom is null)
        {
          return Results.NotFound(new { message = "Classroom not found" });
        }

        if (classroom.TeacherId != userId && !user.IsInRole("admin"))
        {
          return Results.Json(new { message = "You do not manage this classroom" }, statusCode: StatusCodes.Status403Forbidden);
        }

        var subjects = await unitOfWork.ClassroomRepository.GetClassroomSubjectsAsync(classroomId);
        await unitOfWork.ClassroomRepository.ProvisionClassroomModulesAsync(classroomId, subjects, classroom.TeacherId);
        return Results.Ok(new { success = true, message = "Classroom modules provisioned successfully." });
      }
      catch (UnauthorizedAccessException ex)
      {
        return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
      }
      catch (InvalidOperationException ex)
      {
        return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
      }
    }), _version, "ProvisionClassroomModules", _tag)
    .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapPatch($"{_routePrefix}{{classroomId:int}}", async (
        int classroomId,
        [FromBody] UpdateClassroomRequest request,
        ClaimsPrincipal user,
        ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var thumbnailInfo = ParseThumbnail(request.Thumbnail);
      var result = await sender.Send(new UpdateClassroomCommand(
          classroomId,
          userId,
          user.IsInRole("admin"),
          request.Name,
          request.Subject,
          request.Subjects,
          request.YearLevel,
          request.Description,
          thumbnailInfo));
      return result.ToEndpointResult();
    }), _version, "UpdateClassroom", _tag)
    .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapDelete($"{_routePrefix}{{classroomId:int}}", async (
        int classroomId,
        ClaimsPrincipal user,
        ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new ArchiveClassroomCommand(classroomId, userId, user.IsInRole("admin")));
      return result.ToEndpointResult();
    }), _version, "ArchiveClassroom", _tag)
    .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    // Join classroom
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}join", async ([FromBody] JoinClassroomRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new JoinClassroomCommand(userId, request.JoinCode));
      return result.ToEndpointResult();
    }), _version, "JoinClassroom", _tag).RequireAuthorization();

  }

  private static ClassroomThumbnailRequest? ParseThumbnail(JsonElement? thumbnail)
  {
    if (thumbnail is null || thumbnail.Value.ValueKind == JsonValueKind.Null || thumbnail.Value.ValueKind == JsonValueKind.Undefined)
      return null;

    if (thumbnail.Value.ValueKind == JsonValueKind.String)
    {
      var url = thumbnail.Value.GetString();
      return string.IsNullOrWhiteSpace(url) ? null : new ClassroomThumbnailRequest("UPLOADED", Url: url);
    }

    if (thumbnail.Value.ValueKind != JsonValueKind.Object)
      return null;

    var type = ReadString(thumbnail.Value, "type") ?? "DEFAULT";
    var assetId = ReadString(thumbnail.Value, "assetId");
    var urlValue = ReadString(thumbnail.Value, "url");
    var prompt = ReadString(thumbnail.Value, "prompt");

    return new ClassroomThumbnailRequest(type, assetId, urlValue, prompt);
  }

  private static string? ReadString(JsonElement element, string propertyName)
  {
    if (!element.TryGetProperty(propertyName, out var value))
      return null;

    return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
  }
}
