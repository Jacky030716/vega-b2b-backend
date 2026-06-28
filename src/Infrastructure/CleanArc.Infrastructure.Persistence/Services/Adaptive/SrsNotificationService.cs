using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Notifications;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using CleanArc.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

public class SrsNotificationService(
    ApplicationDbContext dbContext,
    HttpClient httpClient,
    INotificationInboxService notificationInboxService,
    INotificationValidationService notificationValidationService,
    IBackgroundJobManager backgroundJobManager,
    ILogger<SrsNotificationService> logger) : ISrsNotificationService
{
    private const string AlertType = "ACADEMIC_CRITICAL";
    private const string AlertTitle = "Review Needed!";
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonElement EmptyReviewGroups = JsonSerializer.SerializeToElement(Array.Empty<object>(), WebJsonOptions);

    public async Task RegisterPushTokenAsync(int studentId, string token, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync(new object[] { studentId }, cancellationToken);
        if (user != null)
        {
            user.ExpoPushToken = token;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully registered Expo push token for user ID {UserId}.", studentId);
        }
        else
        {
            logger.LogWarning("User ID {UserId} not found when attempting to register push token.", studentId);
        }
    }

    public async Task SendMasteryDecayNotificationsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting SRS mastery decay check and notifications dispatch...");

        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-8);
        var studentsWithOverdue = await dbContext.WordProgresses
            .Where(progress => progress.NextReviewDate.HasValue && progress.NextReviewDate.Value <= now)
            .Select(progress => progress.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (studentsWithOverdue.Count == 0)
        {
            logger.LogInformation("No students found with overdue spaced repetition review items.");
            return;
        }

        // Inbox alerts are available even when push permission has not been granted.
        var studentsToNotify = await dbContext.Users
            .Where(user => studentsWithOverdue.Contains(user.Id)
                && (!user.LastSrsNotificationSentAt.HasValue || user.LastSrsNotificationSentAt.Value <= cutoff))
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Found {Count} users with overdue reviews who have not been notified in the last 8 hours.",
            studentsToNotify.Count);

        foreach (var student in studentsToNotify)
        {
            if (!notificationValidationService.ShouldSendNotification(student, NotificationCategories.PracticeReminder, now))
                continue;

            var overdueCount = await dbContext.WordProgresses.CountAsync(
                progress => progress.StudentId == student.Id
                    && progress.NextReviewDate.HasValue
                    && progress.NextReviewDate.Value <= now,
                cancellationToken);

            if (overdueCount == 0)
                continue;

            var notification = await CreateInboxNotificationAsync(student.Id, now, cancellationToken);
            if (notification is null)
                continue;

            if (!string.IsNullOrEmpty(student.ExpoPushToken))
            {
                var payloadStr = notification.Payload.ValueKind == JsonValueKind.Undefined
                    ? "{}"
                    : notification.Payload.GetRawText();

                await EnqueuePushNotificationAsync(
                    student.Id,
                    student.ExpoPushToken,
                    notification.Id,
                    notification.Type,
                    notification.Title,
                    notification.Body,
                    payloadStr,
                    cancellationToken);
            }

            student.LastSrsNotificationSentAt = now;
        }

        if (studentsToNotify.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Finished dispatching SRS mastery decay notifications.");
    }

    public async Task SendNotificationIfOverdueAsync(int studentId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync(new object[] { studentId }, cancellationToken);
        if (user == null)
            return;

        var today = DateTime.UtcNow.Date;
        if (user.LastSrsNotificationSentAt.HasValue && user.LastSrsNotificationSentAt.Value.Date == today)
            return;

        var now = DateTime.UtcNow;
        if (!notificationValidationService.ShouldSendNotification(user, NotificationCategories.PracticeReminder, now))
            return;

        var overdueCount = await dbContext.WordProgresses.CountAsync(
            progress => progress.StudentId == studentId
                && progress.NextReviewDate.HasValue
                && progress.NextReviewDate.Value <= now,
            cancellationToken);

        if (overdueCount == 0)
            return;

        logger.LogInformation(
            "Student {UserId} logged in with {Count} overdue review items. Creating an inbox alert.",
            studentId,
            overdueCount);

        var notification = await CreateInboxNotificationAsync(studentId, now, cancellationToken);
        if (notification is null)
            return;

        if (!string.IsNullOrEmpty(user.ExpoPushToken))
        {
            var payloadStr = notification.Payload.ValueKind == JsonValueKind.Undefined
                ? "{}"
                : notification.Payload.GetRawText();

            await EnqueuePushNotificationAsync(
                studentId,
                user.ExpoPushToken,
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Body,
                payloadStr,
                cancellationToken);
        }

        user.LastSrsNotificationSentAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SendStreakReminderNudgeAsync(int studentId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync(new object[] { studentId }, cancellationToken);
        if (user == null)
            return;

        if (!notificationValidationService.ShouldSendNotification(user, NotificationCategories.StreakNudge, DateTime.UtcNow))
        {
            logger.LogInformation("Streak nudge skipped for user {UserId} due to preferences, timezone, or quiet hours.", studentId);
            return;
        }

        var notification = await notificationInboxService.CreateAsync(
            new CreateNotificationRequest(
                studentId,
                "Streak Nudge",
                "Keep your streak alive! Practice today.",
                "STREAK_NUDGE",
                "{}",
                $"streak-nudge:{DateTime.UtcNow.Date:yyyyMMdd}"),
            cancellationToken);

        if (!string.IsNullOrEmpty(user.ExpoPushToken))
        {
            await EnqueuePushNotificationAsync(
                studentId,
                user.ExpoPushToken,
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Body,
                "{}",
                cancellationToken);
        }
    }

    public async Task SendWeeklyReportNotificationAsync(int studentId, string reportTitle, string reportLink, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync(new object[] { studentId }, cancellationToken);
        if (user == null)
            return;

        if (!notificationValidationService.ShouldSendNotification(user, NotificationCategories.WeeklyReport, DateTime.UtcNow))
        {
            logger.LogInformation("Weekly report notification skipped for user {UserId} due to preferences, timezone, or quiet hours.", studentId);
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new { link = reportLink }, WebJsonOptions);

        var notification = await notificationInboxService.CreateAsync(
            new CreateNotificationRequest(
                studentId,
                reportTitle,
                "Your weekly progress report is ready. Check out how you performed!",
                "WEEKLY_REPORT",
                payloadJson,
                $"weekly-report:{DateTime.UtcNow.Date:yyyyMMdd}"),
            cancellationToken);

        if (!string.IsNullOrEmpty(user.ExpoPushToken))
        {
            await EnqueuePushNotificationAsync(
                studentId,
                user.ExpoPushToken,
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Body,
                payloadJson,
                cancellationToken);
        }
    }

    public async Task SendAchievementAlertNotificationAsync(int studentId, string badgeName, string badgeDescription, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync(new object[] { studentId }, cancellationToken);
        if (user == null)
            return;

        if (!notificationValidationService.ShouldSendNotification(user, NotificationCategories.AchievementAlert, DateTime.UtcNow))
        {
            logger.LogInformation("Achievement alert skipped for user {UserId} due to preferences, timezone, or quiet hours.", studentId);
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new { badgeName, badgeDescription }, WebJsonOptions);

        var notification = await notificationInboxService.CreateAsync(
            new CreateNotificationRequest(
                studentId,
                "New Achievement Unlocked!",
                $"Congratulations! You earned the {badgeName} badge: {badgeDescription}",
                "ACHIEVEMENT_ALERT",
                payloadJson,
                $"achievement-unlocked:{badgeName}:{studentId}"),
            cancellationToken);

        if (!string.IsNullOrEmpty(user.ExpoPushToken))
        {
            await EnqueuePushNotificationAsync(
                studentId,
                user.ExpoPushToken,
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Body,
                payloadJson,
                cancellationToken);
        }
    }

    public async Task ProcessPushNotificationAttemptAsync(int attemptId, CancellationToken cancellationToken)
    {
        var attempt = await dbContext.PushNotificationAttempts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);

        if (attempt is null)
        {
            logger.LogWarning("Push notification attempt {AttemptId} not found.", attemptId);
            return;
        }

        if (attempt.Status == "Sent")
        {
            logger.LogInformation("Push notification attempt {AttemptId} has already been sent successfully. Skipping to prevent duplicate delivery.", attemptId);
            return;
        }

        // Verify user still has this token
        if (attempt.User.ExpoPushToken != attempt.PushToken || string.IsNullOrEmpty(attempt.PushToken))
        {
            attempt.Status = "DeliveryFailed";
            attempt.ErrorMessage = "Token mismatch or empty.";
            attempt.ModifiedDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var notification = await dbContext.UserNotifications.FindAsync(new object[] { attempt.AlertId }, cancellationToken);
        if (notification is null)
        {
            attempt.Status = "DeliveryFailed";
            attempt.ErrorMessage = "Associated UserNotification not found.";
            attempt.ModifiedDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            var reviewRoute = $"/(student)/notification-review/{notification.Id}";
            var payload = new
            {
                to = attempt.PushToken,
                sound = "default",
                title = notification.Title,
                body = notification.Body,
                data = new
                {
                    alertId = notification.Id,
                    screen = notification.AlertType == "ACADEMIC_CRITICAL" ? "NotificationReview" : "Notifications",
                    link = reviewRoute,
                    alertType = notification.AlertType,
                    overdueCount = ExtractOverdueCount(notification.PayloadJson),
                    moduleCount = ExtractModuleCount(notification.PayloadJson),
                    reviewGroups = ExtractReviewGroups(notification.PayloadJson)
                }
            };

            var response = await httpClient.PostAsJsonAsync(
                "https://exp.host/--/api/v2/push/send",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Expo API returned non-success status code {response.StatusCode}. Content: {content}");
            }

            var responseBody = await response.Content.ReadFromJsonAsync<ExpoPushResponse>(cancellationToken: cancellationToken);
            if (responseBody?.Data == null || responseBody.Data.Count == 0)
            {
                throw new Exception("Expo API returned an empty or invalid response.");
            }

            var ticket = responseBody.Data[0];
            if (ticket.Status == "ok")
            {
                attempt.Status = "Sent";
                attempt.ErrorMessage = null;
                attempt.ModifiedDate = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Successfully sent push notification for attempt {AttemptId}.", attemptId);
            }
            else
            {
                var isPermanent = ticket.Details?.Error == "DeviceNotRegistered" || ticket.Details?.Error == "InvalidCredentials";
                attempt.ErrorMessage = ticket.Message ?? ticket.Details?.Error ?? "Unknown Expo Error";
                attempt.Status = "DeliveryFailed";
                attempt.ModifiedDate = DateTime.UtcNow;

                if (isPermanent)
                {
                    // Clear user's push token
                    attempt.User.ExpoPushToken = null;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    logger.LogWarning("Permanent push failure for user {UserId}. Cleared invalid token. Message: {Message}", attempt.UserId, attempt.ErrorMessage);
                }
                else
                {
                    attempt.RetryCount++;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    throw new Exception($"Temporary Expo Error (Attempt {attempt.RetryCount}): {attempt.ErrorMessage}");
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            attempt.Status = "DeliveryFailed";
            attempt.ErrorMessage = ex.Message;
            attempt.RetryCount++;
            attempt.ModifiedDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogError(ex, "Error processing push notification attempt {AttemptId}.", attemptId);
            throw;
        }
    }

    private async Task EnqueuePushNotificationAsync(
        int userId,
        string pushToken,
        int alertId,
        string alertType,
        string title,
        string body,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var attempt = new PushNotificationAttempt
        {
            UserId = userId,
            PushToken = pushToken,
            AlertId = alertId,
            Status = "DeviceTokenSelected",
            RetryCount = 0,
            ErrorMessage = null,
            CreatedTime = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        dbContext.PushNotificationAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);

        backgroundJobManager.EnqueuePushNotification(attempt.Id);
    }

    private async Task<NotificationDto?> CreateInboxNotificationAsync(
        int studentId,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        var snapshot = await BuildReviewSnapshotAsync(studentId, timestamp, cancellationToken);
        if (snapshot is null)
            return null;

        var eightHourBucket = timestamp.Ticks / TimeSpan.FromHours(8).Ticks;
        var placeholderLink = "/(student)/notification-review";

        var created = await notificationInboxService.CreateAsync(
            new CreateNotificationRequest(
                studentId,
                AlertTitle,
                BuildBody(snapshot.OverdueCount),
                AlertType,
                BuildPayloadJson(snapshot, placeholderLink),
                $"srs-overdue:{eightHourBucket}"),
            cancellationToken);

        await UpdateStoredNotificationPayloadAsync(created.Id, snapshot, cancellationToken);

        return await notificationInboxService.GetByIdAsync(created.Id, studentId, cancellationToken) ?? created;
    }

    private async Task UpdateStoredNotificationPayloadAsync(
        int notificationId,
        ReviewSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.UserNotifications.FirstOrDefaultAsync(
            row => row.Id == notificationId,
            cancellationToken);

        if (notification is null)
            return;

        notification.PayloadJson = BuildPayloadJson(snapshot, $"/(student)/notification-review/{notificationId}");
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ReviewSnapshot?> BuildReviewSnapshotAsync(
        int studentId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var overdueProgresses = await dbContext.WordProgresses
            .AsNoTracking()
            .Where(progress => progress.StudentId == studentId
                && progress.NextReviewDate.HasValue
                && progress.NextReviewDate.Value <= now)
            .Include(progress => progress.Word)
                .ThenInclude(word => word.Module)
                    .ThenInclude(module => module.ClassroomModules)
                        .ThenInclude(link => link.Classroom)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        if (overdueProgresses.Count == 0)
            return null;

        var studentClassrooms = await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(student => student.UserId == studentId && student.Classroom.IsActive && !student.Classroom.IsDeleted)
            .Select(student => new { student.ClassroomId, student.Classroom.Name })
            .ToDictionaryAsync(item => item.ClassroomId, item => item.Name, cancellationToken);

        var eligibleClassroomIds = studentClassrooms.Keys.ToHashSet();

        var reviewGroups = overdueProgresses
            .GroupBy(progress => progress.Word.ModuleId)
            .Select(group =>
            {
                var first = group.First();
                var module = first.Word.Module;
                var classroomLink =
                    module.ClassroomModules
                        .Where(link => eligibleClassroomIds.Contains(link.ClassroomId))
                        .OrderBy(link => link.ClassroomId)
                        .FirstOrDefault()
                    ?? module.ClassroomModules
                        .OrderBy(link => link.ClassroomId)
                        .FirstOrDefault();

                var classroomId = classroomLink?.ClassroomId ?? 0;
                var classroomName = classroomLink?.Classroom?.Name
                    ?? (classroomId > 0 && studentClassrooms.TryGetValue(classroomId, out var classroomTitle)
                        ? classroomTitle
                        : string.Empty);

                return new ReviewGroupSnapshot(
                    module.Id,
                    module.Title,
                    module.Subject,
                    classroomId,
                    classroomName,
                    group.OrderBy(progress => progress.NextReviewDate ?? DateTime.MaxValue)
                        .ThenBy(progress => progress.Word.DisplayOrder)
                        .ThenBy(progress => progress.Word.Word)
                        .Select(progress => new ReviewWordSnapshot(
                            progress.WordId,
                            progress.Word.Word,
                            progress.NextReviewDate))
                        .ToList());
            })
            .OrderBy(group => group.Words.Count == 0
                ? DateTime.MaxValue
                : group.Words.Min(word => word.NextReviewDate ?? DateTime.MaxValue))
            .ThenBy(group => group.ModuleTitle)
            .ThenBy(group => group.ModuleId)
            .ToList();

        return new ReviewSnapshot(
            overdueProgresses.Count,
            reviewGroups.Count,
            reviewGroups);
    }

    private static string BuildPayloadJson(ReviewSnapshot snapshot, string reviewLink)
    {
        return JsonSerializer.Serialize(new
        {
            screen = "NotificationReview",
            link = reviewLink,
            alertType = AlertType,
            overdueCount = snapshot.OverdueCount,
            moduleCount = snapshot.ModuleCount,
            reviewGroups = snapshot.ReviewGroups.Select(group => new
            {
                moduleId = group.ModuleId,
                moduleTitle = group.ModuleTitle,
                subject = group.Subject,
                classroomId = group.ClassroomId,
                classroomName = group.ClassroomName,
                words = group.Words.Select(word => new
                {
                    wordId = word.WordId,
                    word = word.Word,
                    nextReviewDate = word.NextReviewDate
                })
            })
        }, WebJsonOptions);
    }

    private static int ExtractOverdueCount(JsonElement payload)
    {
        return payload.TryGetProperty("overdueCount", out var element) && element.TryGetInt32(out var count)
            ? count
            : 0;
    }

    private static int ExtractModuleCount(JsonElement payload)
    {
        return payload.TryGetProperty("moduleCount", out var element) && element.TryGetInt32(out var count)
            ? count
            : 0;
    }

    private static JsonElement ExtractReviewGroups(JsonElement payload)
    {
        return payload.TryGetProperty("reviewGroups", out var element) && element.ValueKind == JsonValueKind.Array
            ? element
            : EmptyReviewGroups;
    }

    private static int ExtractOverdueCount(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            return ExtractOverdueCount(doc.RootElement);
        }
        catch { return 0; }
    }

    private static int ExtractModuleCount(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            return ExtractModuleCount(doc.RootElement);
        }
        catch { return 0; }
    }

    private static JsonElement ExtractReviewGroups(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            return ExtractReviewGroups(doc.RootElement).Clone();
        }
        catch { return EmptyReviewGroups; }
    }

    private static string BuildBody(int overdueCount) => overdueCount == 1
        ? "You have 1 spelling word that is overdue for review. Keep your streak alive!"
        : $"You have {overdueCount} spelling words overdue for review. Keep your streak alive!";

    private sealed record ReviewSnapshot(
        int OverdueCount,
        int ModuleCount,
        IReadOnlyList<ReviewGroupSnapshot> ReviewGroups);

    private sealed record ReviewGroupSnapshot(
        int ModuleId,
        string ModuleTitle,
        string Subject,
        int ClassroomId,
        string ClassroomName,
        IReadOnlyList<ReviewWordSnapshot> Words);

    private sealed record ReviewWordSnapshot(
        int WordId,
        string Word,
        DateTime? NextReviewDate);

    public class ExpoPushResponse
    {
        public List<ExpoPushTicket> Data { get; set; } = new();
    }

    public class ExpoPushTicket
    {
        public string Status { get; set; } = string.Empty; // "ok" or "error"
        public string? Id { get; set; }
        public string? Message { get; set; }
        public ExpoPushDetails? Details { get; set; }
    }

    public class ExpoPushDetails
    {
        public string? Error { get; set; } // e.g. "DeviceNotRegistered"
    }
}
