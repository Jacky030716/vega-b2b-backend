using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure.AI;

public static class AiFeatureTypes
{
  public const string ClassroomThumbnailGeneration = "CLASSROOM_THUMBNAIL_GENERATION";
  public const string CustomChallengeGeneration = "CUSTOM_CHALLENGE_GENERATION";
  public const string PredefinedModuleGeneration = "PREDEFINED_MODULE_GENERATION";
  public const string RecoveryMissionPreview = "RECOVERY_MISSION_PREVIEW";
  public const string AdminAuditor = "ADMIN_AUDITOR";
  public const string StickerGeneration = "STICKER_GENERATION";
}

public record AiQuotaResult(int MonthlyLimit, int Used, int Remaining);

public interface IAiUsageService
{
  Task<bool> CanUseFeatureAsync(int userId, string featureType, CancellationToken cancellationToken);
  Task ConsumeUsageAsync(
    int userId,
    string featureType,
    string endpointKey,
    string provider,
    string? modelName,
    int requestCount,
    bool success,
    string? errorCode,
    string? relatedEntityType,
    int? relatedEntityId,
    CancellationToken cancellationToken);
  Task<AiQuotaResult> GetRemainingQuotaAsync(int userId, string featureType, CancellationToken cancellationToken);
}

public interface IAiRateLimitService
{
  Task<(bool Allowed, int RetryAfterSeconds)> TryAcquireAsync(
    int userId,
    string featureType,
    CancellationToken cancellationToken);
}
