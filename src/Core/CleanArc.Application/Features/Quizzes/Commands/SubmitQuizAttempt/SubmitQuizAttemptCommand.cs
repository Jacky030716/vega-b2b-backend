using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Commands.SubmitQuizAttempt;

public record SubmitQuizAttemptCommand(int UserId, string QuizId, string AttemptId, SubmitQuizAttemptRequest Request)
    : IRequest<OperationResult<SubmitQuizAttemptResponse>>, IValidatableModel<SubmitQuizAttemptCommand>
{
  public IValidator<SubmitQuizAttemptCommand> ValidateApplicationModel(ApplicationBaseValidationModelProvider<SubmitQuizAttemptCommand> validator)
  {
    validator.RuleFor(c => c.UserId)
        .GreaterThan(0)
        .WithMessage("Invalid user ID");

    validator.RuleFor(c => c.QuizId)
        .NotEmpty()
        .WithMessage("QuizId is required");

    validator.RuleFor(c => c.AttemptId)
        .NotEmpty()
        .WithMessage("AttemptId is required");

    validator.RuleFor(c => c.Request)
        .NotNull()
        .WithMessage("Request is required");

    return validator;
  }
}
