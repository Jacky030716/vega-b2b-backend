using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Users.Commands.ResetStudentNotificationPreferences;

public record ResetStudentNotificationPreferencesCommand(int StudentId)
    : IRequest<OperationResult<StudentNotificationPreferencesDto>>,
      IValidatableModel<ResetStudentNotificationPreferencesCommand>
{
    public IValidator<ResetStudentNotificationPreferencesCommand> ValidateApplicationModel(
        ApplicationBaseValidationModelProvider<ResetStudentNotificationPreferencesCommand> validator)
    {
        validator.RuleFor(model => model.StudentId).GreaterThan(0);
        return validator;
    }
}
