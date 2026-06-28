using CleanArc.Application.Contracts.Notifications;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence.Services.Notifications;
using Xunit;

namespace CleanArc.Tests.Setup.Features.Notifications;

public class NotificationValidationServiceTests
{
    private readonly NotificationValidationService _service = new();

    [Fact]
    public void ShouldSendNotification_WhenInAppNotificationsDisabled_ReturnsFalse()
    {
        var user = new User
        {
            InAppNotificationsEnabled = false,
            PracticeRemindersEnabled = true
        };

        var result = _service.ShouldSendNotification(user, NotificationCategories.PracticeReminder, DateTime.UtcNow);

        Assert.False(result);
    }

    [Fact]
    public void ShouldSendNotification_WhenCategoryDisabled_ReturnsFalse()
    {
        var user = new User
        {
            InAppNotificationsEnabled = true,
            StreakRemindersEnabled = false
        };

        var result = _service.ShouldSendNotification(user, NotificationCategories.StreakNudge, DateTime.UtcNow);

        Assert.False(result);
    }

    [Fact]
    public void ShouldSendNotification_WhenWithinQuietHours_ReturnsFalse()
    {
        // Quiet hours: 22:00 to 08:00
        var user = new User
        {
            InAppNotificationsEnabled = true,
            StreakRemindersEnabled = true,
            QuietHoursStartLocal = "22:00",
            QuietHoursEndLocal = "08:00",
            NotificationTimezone = "UTC"
        };

        // 23:00 UTC
        var utcTime = new DateTime(2026, 6, 26, 23, 0, 0, DateTimeKind.Utc);
        var result = _service.ShouldSendNotification(user, NotificationCategories.StreakNudge, utcTime);

        Assert.False(result);
    }

    [Fact]
    public void ShouldSendNotification_WhenOutsideQuietHours_ReturnsTrue()
    {
        var user = new User
        {
            InAppNotificationsEnabled = true,
            StreakRemindersEnabled = true,
            QuietHoursStartLocal = "22:00",
            QuietHoursEndLocal = "08:00",
            NotificationTimezone = "UTC"
        };

        // 12:00 UTC (noon)
        var utcTime = new DateTime(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
        var result = _service.ShouldSendNotification(user, NotificationCategories.StreakNudge, utcTime);

        Assert.True(result);
    }

    [Fact]
    public void ShouldSendNotification_ForPracticeReminder_BeforeReminderTime_ReturnsFalse()
    {
        var user = new User
        {
            InAppNotificationsEnabled = true,
            PracticeRemindersEnabled = true,
            ReminderTimeLocal = "18:00",
            QuietHoursStartLocal = "22:00",
            QuietHoursEndLocal = "08:00",
            NotificationTimezone = "UTC"
        };

        // 17:00 UTC
        var utcTime = new DateTime(2026, 6, 26, 17, 0, 0, DateTimeKind.Utc);
        var result = _service.ShouldSendNotification(user, NotificationCategories.PracticeReminder, utcTime);

        Assert.False(result);
    }

    [Fact]
    public void ShouldSendNotification_ForPracticeReminder_AfterReminderTime_ReturnsTrue()
    {
        var user = new User
        {
            InAppNotificationsEnabled = true,
            PracticeRemindersEnabled = true,
            ReminderTimeLocal = "18:00",
            QuietHoursStartLocal = "22:00",
            QuietHoursEndLocal = "08:00",
            NotificationTimezone = "UTC"
        };

        // 19:00 UTC
        var utcTime = new DateTime(2026, 6, 26, 19, 0, 0, DateTimeKind.Utc);
        var result = _service.ShouldSendNotification(user, NotificationCategories.PracticeReminder, utcTime);

        Assert.True(result);
    }
}
