using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Classrooms.Commands;
using CleanArc.Application.Features.Classrooms.Queries;
using CleanArc.Application.Features.Games.Queries;
using CleanArc.Web.Api.Contracts.Requests.Classrooms;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

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

    // Get teacher classrooms
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}teacher", async (ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetTeacherClassroomsQuery(userId));
      return result.ToEndpointResult();
    }), _version, "GetTeacherClassrooms", _tag).RequireAuthorization();

    // Get classroom detail
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{classroomId}}", async (int classroomId, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetClassroomDetailQuery(classroomId, userId));
      return result.ToEndpointResult();
    }), _version, "GetClassroomDetail", _tag).RequireAuthorization();

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
      var result = await sender.Send(new CreateClassroomCommand(userId, request.Name, request.Description, request.Subject, request.Thumbnail));
      return result.ToEndpointResult();
    }), _version, "CreateClassroom", _tag).RequireAuthorization();

    // Join classroom
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}join", async ([FromBody] JoinClassroomRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new JoinClassroomCommand(userId, request.JoinCode));
      return result.ToEndpointResult();
    }), _version, "JoinClassroom", _tag).RequireAuthorization();

  }
}
