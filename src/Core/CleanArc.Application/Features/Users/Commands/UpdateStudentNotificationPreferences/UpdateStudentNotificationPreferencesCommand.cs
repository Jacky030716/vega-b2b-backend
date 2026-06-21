using System.Globalization;
using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Users.Commands.UpdateStudentNotificationPreferences;

public record UpdateStudentNotificationPreferencesCommand(
    int StudentId,
    bool InAppNotificationsEnabled,
    bool PracticeRemindersEnabled,
    bool StreakRemindersEnabled,
    bool AchievementAlertsEnabled,
    bool WeeklyReportsEnabled,
    string ReminderTimeLocal,
    string QuietHoursStartLocal,
    string QuietHoursEndLocal,
    string NotificationTimezone)
    : IRequest<OperationResult<StudentNotificationPreferencesDto>>,
      IValidatableModel<UpdateStudentNotificationPreferencesCommand>
{
    public IValidator<UpdateStudentNotificationPreferencesCommand> ValidateApplicationModel(
        ApplicationBaseValidationModelProvider<UpdateStudentNotificationPreferencesCommand> validator)
    {
        validator.RuleFor(model => model.StudentId).GreaterThan(0);
        validator.RuleFor(model => model.ReminderTimeLocal)
            .Must(BeValidTime).WithMessage("Reminder time must use HH:mm format.");
        validator.RuleFor(model => model.QuietHoursStartLocal)
            .Must(BeValidTime).WithMessage("Quiet hours start must use HH:mm format.");
        validator.RuleFor(model => model.QuietHoursEndLocal)
            .Must(BeValidTime).WithMessage("Quiet hours end must use HH:mm format.");
        validator.RuleFor(model => model.NotificationTimezone)
            .NotEmpty()
            .WithMessage("Notification timezone is required.");
        return validator;
    }

    internal static bool BeValidTime(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }
}
