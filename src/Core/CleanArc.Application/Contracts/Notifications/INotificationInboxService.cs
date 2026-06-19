using System.Text.Json;

namespace CleanArc.Application.Contracts.Notifications;

public record NotificationDto(
    int Id,
    string Title,
    string Body,
    DateTime Timestamp,
    string Type,
    bool IsRead,
    JsonElement Payload);

public record CreateNotificationRequest(
    int UserId,
    string Title,
    string Body,
    string AlertType,
    string PayloadJson,
    string? DeduplicationKey = null);

public interface INotificationInboxService
{
    Task<IReadOnlyList<NotificationDto>> GetLatestAsync(
        int userId,
        int limit,
        CancellationToken cancellationToken);

    Task<NotificationDto?> GetByIdAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken);

    Task<NotificationDto> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken);

    Task<bool> MarkAsReadAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken);
}
