using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Features.Adaptive.Analytics;
using CleanArc.Application.Features.Adaptive.Attempts;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class AttemptTrackingEndpoints : ICarterModule
{
  private const string RoutePrefix = "/api/v{version:apiVersion}/";
  private const double Version = 1.1;
  private const string Tag = "Adaptive Attempts";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}attempts/start",
      async ([FromBody] StartAdaptiveAttemptRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
      {
        try
        {
          var userId = int.Parse(user.Identity!.GetUserId());
          var result = await sender.Send(new StartAdaptiveAttemptCommand(request, userId), cancellationToken);
          return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "StartAdaptiveAttempt", Tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}attempts/item",
      async ([FromBody] SubmitAdaptiveItemAttemptRequest request, ISender sender, CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new RecordAdaptiveItemAttemptCommand(request), cancellationToken);
        return Results.Ok(result);
      }), Version, "RecordAdaptiveItemAttempt", Tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}attempts/complete",
      async ([FromBody] CompleteAdaptiveAttemptRequest request, ISender sender, CancellationToken cancellationToken) =>
      {
        try
        {
          await sender.Send(new CompleteAdaptiveAttemptCommand(request), cancellationToken);
          return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "CompleteAdaptiveAttempt", Tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}students/{{studentId:int}}/mastery",
      async (int studentId, ISender sender, CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new GetStudentMasteryQuery(studentId), cancellationToken);
        return Results.Ok(result);
      }), Version, "GetStudentMastery", Tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}students/{{studentId:int}}/weakness-summary",
      async (int studentId, ISender sender, CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new GetStudentWeaknessSummaryQuery(studentId), cancellationToken);
        return Results.Ok(result);
      }), Version, "GetStudentWeaknessSummary", Tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}students/{{studentId:int}}/recommended-next-challenges",
      async (int studentId, ISender sender, CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new GetStudentRecommendedNextChallengesQuery(studentId), cancellationToken);
        return Results.Ok(result);
      }), Version, "GetStudentRecommendedNextChallenges", Tag).RequireAuthorization();
  }
}
