using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Features.Adaptive.Orchestration;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class AdaptiveEndpoints : ICarterModule
{
  private const string RoutePrefix = "/api/v{version:apiVersion}/adaptive/";
  private const double Version = 1.1;
  private const string Tag = "Adaptive";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}recommendations/student/{{studentId:int}}",
      async (
        int studentId,
        [FromBody] GenerateAdaptiveChallengeRequest? request,
        ISender sender,
        CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new GetAdaptiveRecommendationsQuery(studentId, request), cancellationToken);
        return Results.Ok(result);
      }), Version, "GetAdaptiveRecommendationsForStudent", Tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}challenges/generate",
      async ([FromBody] GenerateAdaptiveChallengeRequest request, ISender sender, CancellationToken cancellationToken) =>
      {
        try
        {
          var result = await sender.Send(new GenerateAdaptiveChallengeCommand(request), cancellationToken);
          return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "GenerateAdaptiveChallenge", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}challenges/assign",
      async ([FromBody] AssignAdaptiveChallengeRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
      {
        try
        {
          var teacherId = int.Parse(user.Identity!.GetUserId());
          var result = await sender.Send(new AssignAdaptiveChallengeCommand(request, teacherId), cancellationToken);
          return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "AssignAdaptiveChallenge", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}challenges/{{id:int}}",
      async (int id, ISender sender, CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new GetAdaptiveChallengeByIdQuery(id), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
      }), Version, "GetAdaptiveChallenge", Tag).RequireAuthorization();
  }
}
