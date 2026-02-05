using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Users.Commands.ForgotPassword;

public record ResetPasswordCommand(string Email, string ResetToken, string NewPassword)
    : IRequest<OperationResult<bool>>,
    IValidatableModel<ResetPasswordCommand>
{
  public IValidator<ResetPasswordCommand> ValidateApplicationModel(
      ApplicationBaseValidationModelProvider<ResetPasswordCommand> validator)
  {
    validator.RuleFor(c => c.Email)
        .NotEmpty()
        .NotNull()
        .EmailAddress()
        .WithMessage("Valid email address is required");

    validator.RuleFor(c => c.ResetToken)
        .NotEmpty()
        .NotNull()
        .WithMessage("Reset token is required");

    validator.RuleFor(c => c.NewPassword)
        .NotEmpty()
        .NotNull()
        .MinimumLength(8)
        .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")
        .WithMessage("Password must be at least 8 characters with uppercase, lowercase, number, and special character");

    return validator;
  }
}
