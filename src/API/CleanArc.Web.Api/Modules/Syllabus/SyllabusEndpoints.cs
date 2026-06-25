using Carter;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Features.Adaptive.Syllabus;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

#nullable enable

namespace CleanArc.Web.Api.Endpoints;

public class SyllabusEndpoints : ICarterModule
{
  private const string RoutePrefix = "/api/v{version:apiVersion}/syllabus/";
  private const double Version = 1.1;
  private const string Tag = "Syllabus";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}modules",
      async (
        [FromQuery] string? subject,
        [FromQuery] string? language,
        [FromQuery] int? yearLevel,
        ISender sender,
        CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new GetSyllabusModulesQuery(subject, language, yearLevel), cancellationToken);
        return Results.Ok(result);
      }), Version, "GetSyllabusModules", Tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}modules/{{id:int}}",
      async (int id, ISender sender, CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new GetSyllabusModuleByIdQuery(id), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
      }), Version, "GetSyllabusModule", Tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet(
      $"{RoutePrefix}modules/{{id:int}}/vocabulary",
      async (int id, ISender sender, CancellationToken cancellationToken) =>
      {
        var result = await sender.Send(new GetSyllabusModuleVocabularyQuery(id), cancellationToken);
        return Results.Ok(result);
      }), Version, "GetSyllabusModuleVocabulary", Tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}modules",
      async ([FromBody] CreateSyllabusModuleRequest request, ISender sender, CancellationToken cancellationToken) =>
      {
        try
        {
          var result = await sender.Send(new CreateSyllabusModuleCommand(request), cancellationToken);
          return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "CreateSyllabusModule", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}modules/{{id:int}}/vocabulary",
      async (int id, [FromBody] CreateVocabularyItemRequest request, ISender sender, CancellationToken cancellationToken) =>
      {
        try
        {
          var result = await sender.Send(new CreateSyllabusVocabularyItemCommand(id, request), cancellationToken);
          return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "CreateSyllabusModuleVocabulary", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapPut(
      $"{RoutePrefix}modules/{{id:int}}/vocabulary/{{itemId:int}}",
      async (int id, int itemId, [FromBody] CreateVocabularyItemRequest request, ISender sender, CancellationToken cancellationToken) =>
      {
        try
        {
          var result = await sender.Send(new UpdateSyllabusVocabularyItemCommand(id, itemId, request), cancellationToken);
          return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "UpdateSyllabusModuleVocabulary", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapDelete(
      $"{RoutePrefix}modules/{{id:int}}/vocabulary/{{itemId:int}}",
      async (int id, int itemId, ISender sender, CancellationToken cancellationToken) =>
      {
        try
        {
          var result = await sender.Send(new DeleteSyllabusVocabularyItemCommand(id, itemId), cancellationToken);
          return result ? Results.Ok(new { success = true }) : Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { ex.Message } } });
        }
      }), Version, "DeleteSyllabusModuleVocabulary", Tag)
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));
  }
}
