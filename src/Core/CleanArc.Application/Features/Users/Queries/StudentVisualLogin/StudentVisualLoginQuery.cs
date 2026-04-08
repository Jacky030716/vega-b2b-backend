using CleanArc.Application.Models.Common;
using CleanArc.Application.Models.Jwt;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.StudentVisualLogin;

public record StudentVisualLoginQuery(string LoginCode, string VisualSequence)
    : IRequest<OperationResult<AccessToken>>,
      IValidatableModel<StudentVisualLoginQuery>
{
  public IValidator<StudentVisualLoginQuery> ValidateApplicationModel(
      ApplicationBaseValidationModelProvider<StudentVisualLoginQuery> validator)
  {
    validator.RuleFor(c => c.LoginCode)
        .NotEmpty()
        .WithMessage("Login code is required");

    validator.RuleFor(c => c.VisualSequence)
        .NotEmpty()
        .WithMessage("Visual sequence is required");

    return validator;
  }
}
