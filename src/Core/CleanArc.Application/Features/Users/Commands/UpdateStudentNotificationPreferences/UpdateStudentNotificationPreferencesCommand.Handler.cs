using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Features.Users.StudentNotificationPreferences;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Users.Commands.UpdateStudentNotificationPreferences;

internal sealed class UpdateStudentNotificationPreferencesCommandHandler(
    IAppUserManager userManager)
    : IRequestHandler<UpdateStudentNotificationPreferencesCommand, OperationResult<StudentNotificationPreferencesDto>>
{
    public async ValueTask<OperationResult<StudentNotificationPreferencesDto>> Handle(
        UpdateStudentNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var student = await userManager.GetUserByIdAsync(request.StudentId);
        if (student is null)
            return OperationResult<StudentNotificationPreferencesDto>.NotFoundResult("Student not found");

        if (!UpdateStudentNotificationPreferencesCommand.BeValidTime(request.ReminderTimeLocal)
            || !UpdateStudentNotificationPreferencesCommand.BeValidTime(request.QuietHoursStartLocal)
            || !UpdateStudentNotificationPreferencesCommand.BeValidTime(request.QuietHoursEndLocal)
            || string.IsNullOrWhiteSpace(request.NotificationTimezone))
        {
            return OperationResult<StudentNotificationPreferencesDto>.FailureResult(
                "Notification preferences contain invalid time or timezone values.");
        }

        student.InAppNotificationsEnabled = request.InAppNotificationsEnabled;
        student.PracticeRemindersEnabled = request.PracticeRemindersEnabled;
        student.StreakRemindersEnabled = request.StreakRemindersEnabled;
        student.AchievementAlertsEnabled = request.AchievementAlertsEnabled;
        student.WeeklyReportsEnabled = request.WeeklyReportsEnabled;
        student.ReminderTimeLocal = request.ReminderTimeLocal;
        student.QuietHoursStartLocal = request.QuietHoursStartLocal;
        student.QuietHoursEndLocal = request.QuietHoursEndLocal;
        student.NotificationTimezone = request.NotificationTimezone.Trim();

        var updateResult = await userManager.UpdateUser(student);
        if (!updateResult.Succeeded)
        {
            var errorMessage = updateResult.Errors.FirstOrDefault()?.Description
                ?? "Unable to update student notification preferences";
            return OperationResult<StudentNotificationPreferencesDto>.FailureResult(errorMessage);
        }

        return OperationResult<StudentNotificationPreferencesDto>.SuccessResult(
            StudentNotificationPreferencesMapper.FromUser(student));
    }
}
