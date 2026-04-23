using Carter;
using CleanArc.Application.Features.Adaptive.Analytics;
using CleanArc.WebFramework.WebExtensions;
using Mediator;

namespace CleanArc.Web.Api.Endpoints;

public class AdaptiveTeacherEndpoints : ICarterModule
{
  private const string RoutePrefix = "/api/v{version:apiVersion}/teacher/";
  private const double Version = 1.1;
  private const string Tag = "Adaptive Teacher";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}classes/{{classId:int}}/weakness-overview",
      async (int classId, ISender sender, CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new GetClassWeaknessOverviewQuery(classId), cancellationToken);
        return Results.Ok(result);
      }), Version, "GetTeacherClassWeaknessOverview", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}classes/{{classId:int}}/module-progress",
      async (int classId, ISender sender, CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new GetClassModuleProgressQuery(classId), cancellationToken);
        return Results.Ok(result);
      }), Version, "GetTeacherClassModuleProgress", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}students/{{studentId:int}}/performance",
      async (int studentId, ISender sender, CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new GetStudentPerformanceQuery(studentId), cancellationToken);
        return Results.Ok(result);
      }), Version, "GetTeacherStudentPerformance", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));
  }
}
