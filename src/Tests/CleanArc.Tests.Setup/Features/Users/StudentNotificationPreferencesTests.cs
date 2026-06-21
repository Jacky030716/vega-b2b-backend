using CleanArc.Application.Features.Users.Commands.ResetStudentNotificationPreferences;
using CleanArc.Application.Features.Users.Commands.UpdateStudentNotificationPreferences;
using CleanArc.Application.Features.Users.Queries.GetStudentNotificationPreferences;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Users;

public class StudentNotificationPreferencesTests
{
    [Fact]
    public async Task GetStudentNotificationPreferences_ReturnsPersistedSnapshot()
    {
        var student = SetId(new User
        {
            UserName = "student-prefs",
            InAppNotificationsEnabled = false,
            PracticeRemindersEnabled = true,
            StreakRemindersEnabled = false,
            AchievementAlertsEnabled = true,
            WeeklyReportsEnabled = false,
            ReminderTimeLocal = "19:30",
            QuietHoursStartLocal = "21:00",
            QuietHoursEndLocal = "07:15",
            NotificationTimezone = "Asia/Kuala_Lumpur"
        }, 14);

        var userManager = Substitute.For<IAppUserManager>();
        userManager.GetUserByIdAsync(student.Id).Returns(student);

        var handler = new GetStudentNotificationPreferencesQueryHandler(userManager);

        var result = await handler.Handle(
            new GetStudentNotificationPreferencesQuery(student.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Result.InAppNotificationsEnabled);
        Assert.True(result.Result.PracticeRemindersEnabled);
        Assert.False(result.Result.StreakRemindersEnabled);
        Assert.True(result.Result.AchievementAlertsEnabled);
        Assert.False(result.Result.WeeklyReportsEnabled);
        Assert.Equal("19:30", result.Result.ReminderTimeLocal);
        Assert.Equal("21:00", result.Result.QuietHoursStartLocal);
        Assert.Equal("07:15", result.Result.QuietHoursEndLocal);
        Assert.Equal("Asia/Kuala_Lumpur", result.Result.NotificationTimezone);
    }

    [Fact]
    public async Task UpdateStudentNotificationPreferences_PersistsSupportedPreferenceFields()
    {
        var student = SetId(new User
        {
            UserName = "student-update",
            InAppNotificationsEnabled = true,
            PracticeRemindersEnabled = true,
            StreakRemindersEnabled = true,
            AchievementAlertsEnabled = true,
            WeeklyReportsEnabled = true,
            ReminderTimeLocal = "18:00",
            QuietHoursStartLocal = "22:00",
            QuietHoursEndLocal = "08:00",
            NotificationTimezone = "UTC"
        }, 16);

        var userManager = Substitute.For<IAppUserManager>();
        userManager.GetUserByIdAsync(student.Id).Returns(student);
        userManager.UpdateUser(student).Returns(IdentityResult.Success);

        var handler = new UpdateStudentNotificationPreferencesCommandHandler(userManager);

        var result = await handler.Handle(
            new UpdateStudentNotificationPreferencesCommand(
                student.Id,
                false,
                false,
                true,
                false,
                true,
                "20:15",
                "23:30",
                "06:45",
                "Asia/Kuala_Lumpur"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(student.InAppNotificationsEnabled);
        Assert.False(student.PracticeRemindersEnabled);
        Assert.True(student.StreakRemindersEnabled);
        Assert.False(student.AchievementAlertsEnabled);
        Assert.True(student.WeeklyReportsEnabled);
        Assert.Equal("20:15", student.ReminderTimeLocal);
        Assert.Equal("23:30", student.QuietHoursStartLocal);
        Assert.Equal("06:45", student.QuietHoursEndLocal);
        Assert.Equal("Asia/Kuala_Lumpur", student.NotificationTimezone);
        await userManager.Received(1).UpdateUser(student);
    }

    [Fact]
    public async Task ResetStudentNotificationPreferences_RestoresDefaults()
    {
        var student = SetId(new User
        {
            UserName = "student-reset",
            InAppNotificationsEnabled = false,
            PracticeRemindersEnabled = false,
            StreakRemindersEnabled = false,
            AchievementAlertsEnabled = false,
            WeeklyReportsEnabled = false,
            ReminderTimeLocal = "20:15",
            QuietHoursStartLocal = "23:30",
            QuietHoursEndLocal = "06:45",
            NotificationTimezone = "Asia/Kuala_Lumpur"
        }, 18);

        var userManager = Substitute.For<IAppUserManager>();
        userManager.GetUserByIdAsync(student.Id).Returns(student);
        userManager.UpdateUser(student).Returns(IdentityResult.Success);

        var handler = new ResetStudentNotificationPreferencesCommandHandler(userManager);

        var result = await handler.Handle(
            new ResetStudentNotificationPreferencesCommand(student.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(student.InAppNotificationsEnabled);
        Assert.True(student.PracticeRemindersEnabled);
        Assert.True(student.StreakRemindersEnabled);
        Assert.True(student.AchievementAlertsEnabled);
        Assert.True(student.WeeklyReportsEnabled);
        Assert.Equal("18:00", student.ReminderTimeLocal);
        Assert.Equal("22:00", student.QuietHoursStartLocal);
        Assert.Equal("08:00", student.QuietHoursEndLocal);
        Assert.Equal("UTC", student.NotificationTimezone);
        await userManager.Received(1).UpdateUser(student);
    }

    [Fact]
    public async Task UpdateStudentNotificationPreferences_RejectsInvalidReminderTime()
    {
        var student = SetId(new User
        {
            UserName = "student-invalid-time"
        }, 20);

        var userManager = Substitute.For<IAppUserManager>();
        userManager.GetUserByIdAsync(student.Id).Returns(student);

        var handler = new UpdateStudentNotificationPreferencesCommandHandler(userManager);

        var result = await handler.Handle(
            new UpdateStudentNotificationPreferencesCommand(
                student.Id,
                true,
                true,
                true,
                true,
                true,
                "25:00",
                "22:00",
                "08:00",
                "UTC"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        await userManager.DidNotReceive().UpdateUser(Arg.Any<User>());
    }

    private static T SetId<T>(T entity, int id)
    {
        entity!.GetType().GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}
