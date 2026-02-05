using Asp.Versioning;
using CleanArc.Application.Features.Quizzes.Commands.CreateQuizAttempt;
using CleanArc.Application.Features.Quizzes.Commands.SubmitQuizAttempt;
using CleanArc.Application.Features.Quizzes.Commands.UpdateQuizAttempt;
using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Features.Quizzes.Queries.GetQuizById;
using CleanArc.Application.Features.Quizzes.Queries.GetQuizQuestions;
using CleanArc.Application.Features.Quizzes.Queries.GetQuizzes;
using CleanArc.WebFramework.BaseController;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Controllers.V1.Learn;

[ApiVersion("1")]
[ApiController]
[Route("api/v{version:apiVersion}/Quizzes")]
[Authorize]
public class QuizzesController(ISender sender) : BaseController
{
  [HttpGet]
  public async Task<IActionResult> GetQuizzes([FromQuery] string type, [FromQuery] string gameType)
  {
    type ??= string.Empty;
    gameType ??= string.Empty;
    var query = await sender.Send(new GetQuizzesQuery(type, gameType));
    return base.OperationResult(query);
  }

  [HttpGet("{quizId}")]
  public async Task<IActionResult> GetQuizById(string quizId)
  {
    var query = await sender.Send(new GetQuizByIdQuery(quizId));
    return base.OperationResult(query);
  }

  [HttpGet("{quizId}/questions")]
  public async Task<IActionResult> GetQuizQuestions(string quizId)
  {
    var query = await sender.Send(new GetQuizQuestionsQuery(quizId));
    return base.OperationResult(query);
  }

  [HttpPost("{quizId}/attempts")]
  public async Task<IActionResult> CreateAttempt(string quizId, [FromBody] CreateQuizAttemptRequest request)
  {
    var command = await sender.Send(new CreateQuizAttemptCommand(base.UserId, quizId, request));
    return base.OperationResult(command);
  }

  [HttpPatch("{quizId}/attempts/{attemptId}")]
  public async Task<IActionResult> UpdateAttempt(string quizId, string attemptId, [FromBody] UpdateQuizAttemptRequest request)
  {
    var command = await sender.Send(new UpdateQuizAttemptCommand(base.UserId, quizId, attemptId, request));
    return base.OperationResult(command);
  }

  [HttpPost("{quizId}/attempts/{attemptId}/submit")]
  public async Task<IActionResult> SubmitAttempt(string quizId, string attemptId, [FromBody] SubmitQuizAttemptRequest request)
  {
    var command = await sender.Send(new SubmitQuizAttemptCommand(base.UserId, quizId, attemptId, request));
    return base.OperationResult(command);
  }
}
