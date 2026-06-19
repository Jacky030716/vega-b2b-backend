using System.Net.Http.Json;
using System.Text.Json;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Notifications;
using CleanArc.Domain.Entities.Classroom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

public class SrsNotificationService(
    ApplicationDbContext dbContext,
    HttpClient httpClient,
    INotificationInboxService notificationInboxService,
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
                await SendPushNotificationAsync(student.ExpoPushToken, notification.Id, notification, cancellationToken);

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
            await SendPushNotificationAsync(user.ExpoPushToken, notification.Id, notification, cancellationToken);

        user.LastSrsNotificationSentAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
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

    private async Task SendPushNotificationAsync(
        string pushToken,
        int alertId,
        NotificationDto notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var reviewRoute = $"/(student)/notification-review/{alertId}";
            var payload = new
            {
                to = pushToken,
                sound = "default",
                title = AlertTitle,
                body = notification.Body,
                data = new
                {
                    alertId,
                    screen = "NotificationReview",
                    link = reviewRoute,
                    alertType = AlertType,
                    overdueCount = ExtractOverdueCount(notification.Payload),
                    moduleCount = ExtractModuleCount(notification.Payload),
                    reviewGroups = ExtractReviewGroups(notification.Payload)
                }
            };

            var response = await httpClient.PostAsJsonAsync(
                "https://exp.host/--/api/v2/push/send",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "Failed to send Expo push notification. Status: {Status}, Content: {Content}",
                    response.StatusCode,
                    content);
            }
            else
            {
                logger.LogInformation(
                    "Successfully sent decay notification for alert {AlertId} and {Count} words.",
                    alertId,
                    ExtractOverdueCount(notification.Payload));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while sending push notification for alert {AlertId}.", alertId);
        }
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
}
