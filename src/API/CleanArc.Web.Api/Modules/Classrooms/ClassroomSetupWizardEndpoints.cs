using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Classrooms.Commands.SetupClassroom;
using CleanArc.Web.Api.Contracts.Requests.Classrooms;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class ClassroomSetupWizardEndpoints : ICarterModule
{
  private readonly double _version = 1.1;
  private readonly string _tag = "Classrooms";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapPost("/api/v{version:apiVersion}/classrooms/wizard-setup", async (
        [FromForm] SetupClassroomWizardRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken) =>
    {
      var teacherId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new SetupClassroomCommand(
          teacherId,
          request.ClassName,
          request.Subject,
          request.ChallengeId,
          request.CsvContent), cancellationToken);

      return result.ToEndpointResult();
    }).DisableAntiforgery(), _version, "SetupClassroomWizard", _tag).RequireAuthorization();
  }
}
