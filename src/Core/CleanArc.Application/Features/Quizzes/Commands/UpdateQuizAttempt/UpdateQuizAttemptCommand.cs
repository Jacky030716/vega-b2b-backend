using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Commands.UpdateQuizAttempt;

public record UpdateQuizAttemptCommand(int UserId, string QuizId, string AttemptId, UpdateQuizAttemptRequest Request)
    : IRequest<OperationResult<UpdateQuizAttemptResponse>>, IValidatableModel<UpdateQuizAttemptCommand>
{
  public IValidator<UpdateQuizAttemptCommand> ValidateApplicationModel(ApplicationBaseValidationModelProvider<UpdateQuizAttemptCommand> validator)
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

    validator.RuleFor(c => c.Request.QuestionId)
        .NotEmpty()
        .WithMessage("QuestionId is required");

    return validator;
  }
}
