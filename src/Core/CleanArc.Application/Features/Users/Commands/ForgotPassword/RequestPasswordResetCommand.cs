using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Users.Commands.ForgotPassword;

public record RequestPasswordResetCommand(string Email)
    : IRequest<OperationResult<bool>>,
    IValidatableModel<RequestPasswordResetCommand>
{
  public IValidator<RequestPasswordResetCommand> ValidateApplicationModel(
      ApplicationBaseValidationModelProvider<RequestPasswordResetCommand> validator)
  {
    validator.RuleFor(c => c.Email)
        .NotEmpty()
        .NotNull()
        .EmailAddress()
        .WithMessage("Valid email address is required");

    return validator;
  }
}
