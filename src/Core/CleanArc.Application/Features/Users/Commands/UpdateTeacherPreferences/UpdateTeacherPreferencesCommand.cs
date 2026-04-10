using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Users.Commands.UpdateTeacherPreferences;

public record UpdateTeacherPreferencesCommand(
    int TeacherId,
    bool WeeklyAiInsightsEmail,
    bool InactiveStudentAlerts)
    : IRequest<OperationResult<UpdateTeacherPreferencesResult>>,
      IValidatableModel<UpdateTeacherPreferencesCommand>
{
  public IValidator<UpdateTeacherPreferencesCommand> ValidateApplicationModel(
      ApplicationBaseValidationModelProvider<UpdateTeacherPreferencesCommand> validator)
  {
    validator.RuleFor(model => model.TeacherId).GreaterThan(0);
    return validator;
  }
}

public record UpdateTeacherPreferencesResult(
    bool WeeklyAiInsightsEmail,
    bool InactiveStudentAlerts);
