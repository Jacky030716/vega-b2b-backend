using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Classrooms.Commands;
using CleanArc.Application.Features.Classrooms.Queries;
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

    // Get leaderboard
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}leaderboard/{{quizId}}", async (string quizId, [FromQuery] int? classroomId, ISender sender) =>
    {
      var result = await sender.Send(new GetLeaderboardQuery(quizId, classroomId));
      return result.ToEndpointResult();
    }), _version, "GetLeaderboard", _tag).RequireAuthorization();

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

    // Assign quiz to classroom (teacher)
    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}{{classroomId}}/quizzes", async (int classroomId, [FromBody] AssignQuizRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new AssignQuizCommand(classroomId, userId, request.QuizId, request.DueDate));
      return result.ToEndpointResult();
    }), _version, "AssignQuiz", _tag).RequireAuthorization();
  }
}

public record CreateClassroomRequest(string Name, string Description, string Subject, string? Thumbnail);
public record JoinClassroomRequest(string JoinCode);
public record AssignQuizRequest(string QuizId, DateTime? DueDate);
