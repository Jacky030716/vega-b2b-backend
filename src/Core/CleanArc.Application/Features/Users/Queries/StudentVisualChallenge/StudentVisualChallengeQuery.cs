using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.StudentVisualChallenge;

public record StudentVisualChallengeQuery(string ClassCode)
    : IRequest<OperationResult<StudentVisualChallengeResponse>>,
      IValidatableModel<StudentVisualChallengeQuery>
{
  public IValidator<StudentVisualChallengeQuery> ValidateApplicationModel(
      ApplicationBaseValidationModelProvider<StudentVisualChallengeQuery> validator)
  {
    validator.RuleFor(c => c.ClassCode)
        .NotEmpty()
        .WithMessage("Class code is required");

    validator.RuleFor(c => c.ClassCode)
        .Must(code => !string.IsNullOrWhiteSpace(code) && code.Trim().Length == 4)
        .WithMessage("Class code must be 4 characters");

    return validator;
  }
}

public record StudentVisualChallengeResponse(
    int ClassroomId,
    string ClassroomName,
    List<StudentVisualChallengeStudentDto> Students
);

public record StudentVisualChallengeStudentDto(
    int UserId,
    string DisplayName,
    string AvatarId,
    string AvatarUrl,
    string StudentLoginCode
);
