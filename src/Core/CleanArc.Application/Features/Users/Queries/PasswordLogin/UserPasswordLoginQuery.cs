using CleanArc.Application.Models.Common;
using CleanArc.Application.Models.Jwt;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.PasswordLogin;

public record UserPasswordLoginQuery(string UserName, string Password)
    : IRequest<OperationResult<AccessToken>>,
      IValidatableModel<UserPasswordLoginQuery>
{
  public IValidator<UserPasswordLoginQuery> ValidateApplicationModel(
      ApplicationBaseValidationModelProvider<UserPasswordLoginQuery> validator)
  {
    validator.RuleFor(c => c.UserName)
        .NotEmpty()
        .NotNull()
        .WithMessage("Username is required");

    validator.RuleFor(c => c.Password)
        .NotEmpty()
        .NotNull()
        .MinimumLength(8)
        .WithMessage("Password is required");

    return validator;
  }
}
