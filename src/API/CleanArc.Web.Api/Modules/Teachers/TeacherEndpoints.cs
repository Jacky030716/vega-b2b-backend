using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Users.Commands.UpdateTeacherPreferences;
using CleanArc.Application.Features.Users.Queries.GetTeacherProfile;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class TeacherEndpoints : ICarterModule
{
  private const string RoutePrefix = "/api/v{version:apiVersion}/teachers/";
  private const double Version = 1.1;
  private const string Tag = "Teachers";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapGet($"{RoutePrefix}profile", async (
        ClaimsPrincipal user,
        ISender sender) =>
    {
      var teacherId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new GetTeacherProfileQuery(teacherId));
      return result.ToEndpointResult();
    }), Version, "GetTeacherProfile", Tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPut($"{RoutePrefix}preferences", async (
        [FromBody] UpdateTeacherPreferencesRequest request,
        ClaimsPrincipal user,
        ISender sender) =>
    {
      var teacherId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new UpdateTeacherPreferencesCommand(
          teacherId,
          request.WeeklyAiInsightsEmail,
          request.InactiveStudentAlerts));
      return result.ToEndpointResult();
    }), Version, "UpdateTeacherPreferences", Tag).RequireAuthorization();
  }
}

public record UpdateTeacherPreferencesRequest(
    bool WeeklyAiInsightsEmail,
    bool InactiveStudentAlerts);
