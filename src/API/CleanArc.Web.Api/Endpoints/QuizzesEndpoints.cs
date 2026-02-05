using System.Security.Claims;
using Carter;
using CleanArc.Application.Features.Quizzes.Commands.CreateQuizAttempt;
using CleanArc.Application.Features.Quizzes.Commands.SubmitQuizAttempt;
using CleanArc.Application.Features.Quizzes.Commands.UpdateQuizAttempt;
using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Features.Quizzes.Queries.GetQuizById;
using CleanArc.Application.Features.Quizzes.Queries.GetQuizQuestions;
using CleanArc.Application.Features.Quizzes.Queries.GetQuizzes;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Endpoints;

public class QuizzesEndpoints : ICarterModule
{
  private readonly string _routePrefix = "/api/v{version:apiVersion}/Quizzes/";
  private readonly double _version = 1.1;
  private readonly string _tag = "Quizzes";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}", async ([FromQuery] string type, [FromQuery] string gameType, ISender sender) =>
    {
      type ??= string.Empty;
      gameType ??= string.Empty;
      var result = await sender.Send(new GetQuizzesQuery(type, gameType));
      return result.ToEndpointResult();
    }), _version, "GetQuizzes", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{quizId}}", async (string quizId, ISender sender) =>
    {
      var result = await sender.Send(new GetQuizByIdQuery(quizId));
      return result.ToEndpointResult();
    }), _version, "GetQuizById", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapGet($"{_routePrefix}{{quizId}}/questions", async (string quizId, ISender sender) =>
    {
      var result = await sender.Send(new GetQuizQuestionsQuery(quizId));
      return result.ToEndpointResult();
    }), _version, "GetQuizQuestions", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}{{quizId}}/attempts", async (string quizId, [FromBody] CreateQuizAttemptRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new CreateQuizAttemptCommand(userId, quizId, request));
      return result.ToEndpointResult();
    }), _version, "CreateQuizAttempt", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPatch($"{_routePrefix}{{quizId}}/attempts/{{attemptId}}", async (string quizId, string attemptId, [FromBody] UpdateQuizAttemptRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new UpdateQuizAttemptCommand(userId, quizId, attemptId, request));
      return result.ToEndpointResult();
    }), _version, "UpdateQuizAttempt", _tag).RequireAuthorization();

    app.MapEndpoint(builder => builder.MapPost($"{_routePrefix}{{quizId}}/attempts/{{attemptId}}/submit", async (string quizId, string attemptId, [FromBody] SubmitQuizAttemptRequest request, ClaimsPrincipal user, ISender sender) =>
    {
      var userId = int.Parse(user.Identity.GetUserId());
      var result = await sender.Send(new SubmitQuizAttemptCommand(userId, quizId, attemptId, request));
      return result.ToEndpointResult();
    }), _version, "SubmitQuizAttempt", _tag).RequireAuthorization();
  }
}
