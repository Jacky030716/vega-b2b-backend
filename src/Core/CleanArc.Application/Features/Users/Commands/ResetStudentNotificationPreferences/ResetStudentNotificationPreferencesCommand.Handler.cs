using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Features.Users.StudentNotificationPreferences;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.User;
using Mediator;

namespace CleanArc.Application.Features.Users.Commands.ResetStudentNotificationPreferences;

internal sealed class ResetStudentNotificationPreferencesCommandHandler(
    IAppUserManager userManager)
    : IRequestHandler<ResetStudentNotificationPreferencesCommand, OperationResult<StudentNotificationPreferencesDto>>
{
    public async ValueTask<OperationResult<StudentNotificationPreferencesDto>> Handle(
        ResetStudentNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var student = await userManager.GetUserByIdAsync(request.StudentId);
        if (student is null)
            return OperationResult<StudentNotificationPreferencesDto>.NotFoundResult("Student not found");

        student.InAppNotificationsEnabled = StudentNotificationPreferenceDefaults.InAppNotificationsEnabled;
        student.PracticeRemindersEnabled = StudentNotificationPreferenceDefaults.PracticeRemindersEnabled;
        student.StreakRemindersEnabled = StudentNotificationPreferenceDefaults.StreakRemindersEnabled;
        student.AchievementAlertsEnabled = StudentNotificationPreferenceDefaults.AchievementAlertsEnabled;
        student.WeeklyReportsEnabled = StudentNotificationPreferenceDefaults.WeeklyReportsEnabled;
        student.ReminderTimeLocal = StudentNotificationPreferenceDefaults.ReminderTimeLocal;
        student.QuietHoursStartLocal = StudentNotificationPreferenceDefaults.QuietHoursStartLocal;
        student.QuietHoursEndLocal = StudentNotificationPreferenceDefaults.QuietHoursEndLocal;
        student.NotificationTimezone = StudentNotificationPreferenceDefaults.NotificationTimezone;

        var updateResult = await userManager.UpdateUser(student);
        if (!updateResult.Succeeded)
        {
            var errorMessage = updateResult.Errors.FirstOrDefault()?.Description
                ?? "Unable to reset student notification preferences";
            return OperationResult<StudentNotificationPreferencesDto>.FailureResult(errorMessage);
        }

        return OperationResult<StudentNotificationPreferencesDto>.SuccessResult(
            StudentNotificationPreferencesMapper.FromUser(student));
    }
}
