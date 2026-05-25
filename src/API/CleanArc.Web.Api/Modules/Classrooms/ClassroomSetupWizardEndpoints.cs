using System.IO;
using System.Collections.Generic;
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

      var csv = request.CsvContent;
      if (request.CsvFile is not null && request.CsvFile.Length > 0)
      {
        using var reader = new StreamReader(request.CsvFile.OpenReadStream());
        csv = await reader.ReadToEndAsync(cancellationToken);
      }

      var gameKey = request.GameKey ?? "spell_catcher";
      var yearLevel = request.YearLevel ?? 1;
      var subjects = request.Subjects ?? new List<string> { request.Subject };

      var result = await sender.Send(new SetupClassroomCommand(
          teacherId,
          request.ClassName,
          request.Subject,
          gameKey,
          csv,
          yearLevel,
          subjects), cancellationToken);

      return result.ToEndpointResult();
    }).DisableAntiforgery(), _version, "SetupClassroomWizard", _tag).RequireAuthorization();
  }
}
