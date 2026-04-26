using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Features.Challenges.Commands;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public sealed class ModuleManagementEndpoints : ICarterModule
{
  private const string RoutePrefix = "/api/v{version:apiVersion}/";
  private const double Version = 1.1;
  private const string Tag = "Module Challenge Management";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}modules/{{moduleId:int}}/challenges",
      async (
        int moduleId,
        [FromQuery] int classroomId,
        ClaimsPrincipal user,
        IClassroomModuleManagementService service,
        CancellationToken cancellationToken) =>
      {
        try
        {
          var teacherId = int.Parse(user.Identity!.GetUserId());
          return Results.Ok(await service.GetModuleChallengesAsync(moduleId, classroomId, teacherId, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
          return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "GetModuleChallenges", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}modules/{{moduleId:int}}/challenges/generate",
      async (
        int moduleId,
        [FromBody] GenerateModuleChallengeRequest request,
        ClaimsPrincipal user,
        IClassroomModuleManagementService service,
        CancellationToken cancellationToken) =>
      {
        try
        {
          var teacherId = int.Parse(user.Identity!.GetUserId());
          return Results.Ok(await service.GenerateModuleChallengeAsync(moduleId, request, teacherId, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
          return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "GenerateModuleChallenge", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}custom-modules/{{customModuleId:int}}/challenges",
      async (
        int customModuleId,
        ClaimsPrincipal user,
        IClassroomModuleManagementService service,
        CancellationToken cancellationToken) =>
      {
        try
        {
          var teacherId = int.Parse(user.Identity!.GetUserId());
          return Results.Ok(await service.GetCustomModuleChallengesAsync(customModuleId, teacherId, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
          return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "GetCustomModuleChallenges", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapPatch(
      $"{RoutePrefix}custom-modules/{{customModuleId:int}}",
      async (
        int customModuleId,
        [FromBody] RenameCustomModuleRequest request,
        ClaimsPrincipal user,
        IClassroomModuleManagementService service,
        CancellationToken cancellationToken) =>
      {
        try
        {
          var teacherId = int.Parse(user.Identity!.GetUserId());
          return Results.Ok(await service.RenameCustomModuleAsync(customModuleId, request, teacherId, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
          return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "RenameCustomModule", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}custom-modules/{{customModuleId:int}}/challenges",
      async (
        int customModuleId,
        [FromBody] CreateCustomModuleChallengeRequest request,
        ClaimsPrincipal user,
        IClassroomModuleManagementService service,
        CancellationToken cancellationToken) =>
      {
        try
        {
          var teacherId = int.Parse(user.Identity!.GetUserId());
          return Results.Ok(await service.CreateCustomModuleChallengeAsync(customModuleId, request, teacherId, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
          return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "CreateCustomModuleChallenge", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapDelete(
      $"{RoutePrefix}challenges/{{challengeId:int}}",
      async (
        int challengeId,
        ClaimsPrincipal user,
        IClassroomModuleManagementService service,
        CancellationToken cancellationToken) =>
      {
        try
        {
          var teacherId = int.Parse(user.Identity!.GetUserId());
          return Results.Ok(await service.DeleteChallengeAsync(challengeId, teacherId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "DeleteChallenge", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));
  }
}
