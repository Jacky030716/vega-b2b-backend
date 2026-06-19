using System.Text.Json;
using CleanArc.Application.Contracts.Notifications;
using CleanArc.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.Notifications;

public class NotificationInboxService(ApplicationDbContext dbContext) : INotificationInboxService
{
    private static readonly JsonElement EmptyPayload = JsonSerializer.SerializeToElement(new { });

    public async Task<IReadOnlyList<NotificationDto>> GetLatestAsync(
        int userId,
        int limit,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit, 1, 100);
        var rows = await dbContext.UserNotifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedTime)
            .ThenByDescending(notification => notification.Id)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    public async Task<NotificationDto?> GetByIdAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.UserNotifications
            .AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.Id == notificationId && row.UserId == userId,
                cancellationToken);

        return notification is null ? null : ToDto(notification);
    }

    public async Task<NotificationDto> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.DeduplicationKey))
        {
            var existing = await dbContext.UserNotifications
                .AsNoTracking()
                .FirstOrDefaultAsync(notification =>
                    notification.UserId == request.UserId &&
                    notification.DeduplicationKey == request.DeduplicationKey,
                    cancellationToken);

            if (existing is not null)
                return ToDto(existing);
        }

        var notification = new UserNotification
        {
            UserId = request.UserId,
            Title = request.Title,
            Body = request.Body,
            AlertType = request.AlertType,
            PayloadJson = NormalizePayload(request.PayloadJson),
            DeduplicationKey = request.DeduplicationKey
        };

        dbContext.UserNotifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(notification);
    }

    public async Task<bool> MarkAsReadAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.UserNotifications.FirstOrDefaultAsync(
            row => row.Id == notificationId && row.UserId == userId,
            cancellationToken);

        if (notification is null)
            return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<bool> DeleteAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.UserNotifications.FirstOrDefaultAsync(
            row => row.Id == notificationId && row.UserId == userId,
            cancellationToken);

        if (notification is null)
            return false;

        dbContext.UserNotifications.Remove(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static NotificationDto ToDto(UserNotification notification) => new(
        notification.Id,
        notification.Title,
        notification.Body,
        notification.CreatedTime,
        notification.AlertType,
        notification.IsRead,
        ParsePayload(notification.PayloadJson));

    private static string NormalizePayload(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.GetRawText()
                : "{}";
        }
        catch (JsonException)
        {
            return "{}";
        }
    }

    private static JsonElement ParsePayload(string payloadJson)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(payloadJson);
        }
        catch (JsonException)
        {
            return EmptyPayload;
        }
    }
}
