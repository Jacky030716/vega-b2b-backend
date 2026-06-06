using System.Net.Http.Json;
using CleanArc.Application.Contracts.Adaptive;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

public class SrsNotificationService(
    ApplicationDbContext dbContext,
    HttpClient httpClient,
    ILogger<SrsNotificationService> logger) : ISrsNotificationService
{
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

        // Find student IDs who have at least one word progress overdue
        var studentsWithOverdue = await dbContext.WordProgresses
            .Where(wp => wp.NextReviewDate.HasValue && wp.NextReviewDate.Value <= now)
            .Select(wp => wp.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (studentsWithOverdue.Count == 0)
        {
            logger.LogInformation("No students found with overdue spaced repetition review items.");
            return;
        }

        // Query students who have registered a push token AND haven't been notified in the last 8 hours
        var studentsToNotify = await dbContext.Users
            .Where(u => studentsWithOverdue.Contains(u.Id) 
                     && !string.IsNullOrEmpty(u.ExpoPushToken)
                     && (!u.LastSrsNotificationSentAt.HasValue || u.LastSrsNotificationSentAt.Value <= cutoff))
            .ToListAsync(cancellationToken);

        logger.LogInformation("Found {Count} users with registered push tokens, overdue reviews, and not yet notified in the last 8 hours.", studentsToNotify.Count);

        foreach (var student in studentsToNotify)
        {
            var overdueCount = await dbContext.WordProgresses
                .CountAsync(wp => wp.StudentId == student.Id && wp.NextReviewDate.HasValue && wp.NextReviewDate.Value <= now, cancellationToken);

            if (overdueCount == 0) continue;

            await SendPushNotificationAsync(student.ExpoPushToken!, overdueCount, cancellationToken);
            student.LastSrsNotificationSentAt = DateTime.UtcNow;
        }

        if (studentsToNotify.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Finished dispatching SRS mastery decay notifications.");
    }

    public async Task SendNotificationIfOverdueAsync(int studentId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FindAsync(new object[] { studentId }, cancellationToken);
        if (user == null || string.IsNullOrEmpty(user.ExpoPushToken)) return;

        var today = DateTime.UtcNow.Date;
        if (user.LastSrsNotificationSentAt.HasValue && user.LastSrsNotificationSentAt.Value.Date == today)
        {
            return; // Already notified today
        }

        var now = DateTime.UtcNow;
        var overdueCount = await dbContext.WordProgresses
            .CountAsync(wp => wp.StudentId == studentId && wp.NextReviewDate.HasValue && wp.NextReviewDate.Value <= now, cancellationToken);

        if (overdueCount > 0)
        {
            logger.LogInformation("Student {UserId} logged in. Overdue count is {Count}. Dispatching push notification...", studentId, overdueCount);
            await SendPushNotificationAsync(user.ExpoPushToken, overdueCount, cancellationToken);
            
            user.LastSrsNotificationSentAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SendPushNotificationAsync(string pushToken, int overdueCount, CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                to = pushToken,
                sound = "default",
                title = "Review Needed! 📚",
                body = overdueCount == 1 
                    ? "You have 1 spelling word that is overdue for review. Keep your streak alive!" 
                    : $"You have {overdueCount} spelling words overdue for review. Keep your streak alive!",
                data = new { screen = "RecoveryMission" }
            };

            var response = await httpClient.PostAsJsonAsync(
                "https://exp.host/--/api/v2/push/send",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Failed to send Expo push notification to {Token}. Status: {Status}, Content: {Content}", 
                    pushToken, response.StatusCode, content);
            }
            else
            {
                logger.LogInformation("Successfully sent decay notification to {Token} for {Count} words.", pushToken, overdueCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while sending push notification to token {Token}", pushToken);
        }
    }
}
