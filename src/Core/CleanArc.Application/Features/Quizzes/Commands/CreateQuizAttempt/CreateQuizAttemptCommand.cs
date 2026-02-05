using CleanArc.Application.Features.Quizzes.Models;
using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Quizzes.Commands.CreateQuizAttempt;

public record CreateQuizAttemptCommand(int UserId, string QuizId, CreateQuizAttemptRequest Request)
    : IRequest<OperationResult<CreateQuizAttemptResponse>>, IValidatableModel<CreateQuizAttemptCommand>
{
  public IValidator<CreateQuizAttemptCommand> ValidateApplicationModel(ApplicationBaseValidationModelProvider<CreateQuizAttemptCommand> validator)
  {
    validator.RuleFor(c => c.UserId)
        .GreaterThan(0)
        .WithMessage("Invalid user ID");

    validator.RuleFor(c => c.QuizId)
        .NotEmpty()
        .WithMessage("QuizId is required");

    validator.RuleFor(c => c.Request)
        .NotNull()
        .WithMessage("Request is required");

    return validator;
  }
}
