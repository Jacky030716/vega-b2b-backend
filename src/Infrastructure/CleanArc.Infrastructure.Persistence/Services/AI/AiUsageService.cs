using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Domain.Entities.AI;
using CleanArc.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CleanArc.Infrastructure.Persistence;

namespace CleanArc.Infrastructure.Persistence.Services.AI;

public sealed class AiUsageService(
  ApplicationDbContext dbContext,
  IOptions<AiUsageLimitOptions> options,
  ILogger<AiUsageService> logger) : IAiUsageService
{
  private readonly AiUsageLimitOptions _options = options.Value;

  public async Task<bool> CanUseFeatureAsync(int userId, string featureType, CancellationToken cancellationToken)
  {
    var quota = await GetRemainingQuotaAsync(userId, featureType, cancellationToken);
    return quota.Remaining > 0;
  }

  public async Task ConsumeUsageAsync(
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
    CancellationToken cancellationToken)
  {
    var entry = new AiUsageLog
    {
      UserId = userId,
      FeatureType = featureType,
      EndpointKey = endpointKey,
      Provider = provider,
      ModelName = modelName,
      RequestCount = Math.Max(1, requestCount),
      Success = success,
      ErrorCode = errorCode,
      RelatedEntityType = relatedEntityType,
      RelatedEntityId = relatedEntityId,
      CreatedAt = DateTime.UtcNow,
    };

    await dbContext.AiUsageLogs.AddAsync(entry, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<AiQuotaResult> GetRemainingQuotaAsync(int userId, string featureType, CancellationToken cancellationToken)
  {
    var tier = await GetQuotaTierAsync(userId, cancellationToken);
    var monthlyLimit = GetMonthlyLimit(tier, featureType);

    var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    var used = await dbContext.AiUsageLogs
      .AsNoTracking()
      .Where(x => x.UserId == userId && x.FeatureType == featureType && x.Success && x.CreatedAt >= monthStart)
      .SumAsync(x => x.RequestCount, cancellationToken);

    var remaining = Math.Max(0, monthlyLimit - used);
    return new AiQuotaResult(monthlyLimit, used, remaining);
  }

  private async Task<string> GetQuotaTierAsync(int userId, CancellationToken cancellationToken)
  {
    var user = await dbContext.Users
      .AsNoTracking()
      .Include(x => x.Institution)
      .Include(x => x.UserRoles)
        .ThenInclude(x => x.Role)
      .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    if (user is null)
    {
      return "Standard";
    }

    if (HasAdminRole(user))
    {
      return "Admin";
    }

    return user.Institution?.SubscriptionTier ?? "Standard";
  }

  private int GetMonthlyLimit(string tier, string featureType)
  {
    var normalizedTier = string.IsNullOrWhiteSpace(tier) ? "Standard" : tier.Trim();
    var imageFeature = IsImageFeature(featureType);

    return normalizedTier.ToUpperInvariant() switch
    {
      "ADMIN" => imageFeature ? _options.AdminImageMonthlyLimit : _options.AdminTextMonthlyLimit,
      "PLUS" => imageFeature ? _options.PlusImageMonthlyLimit : _options.PlusTextMonthlyLimit,
      "PREMIUM" => imageFeature ? _options.PremiumImageMonthlyLimit : _options.PremiumTextMonthlyLimit,
      _ => imageFeature ? _options.StandardImageMonthlyLimit : _options.StandardTextMonthlyLimit,
    };
  }

  private static bool HasAdminRole(User user)
  {
    return user.UserRoles?.Any(userRole =>
      userRole.Role is not null
      && RoleNames.IsAdmin(userRole.Role.Name)) == true;
  }

  private static bool IsImageFeature(string featureType)
  {
    return string.Equals(featureType, AiFeatureTypes.ClassroomThumbnailGeneration, StringComparison.OrdinalIgnoreCase)
      || string.Equals(featureType, AiFeatureTypes.StickerGeneration, StringComparison.OrdinalIgnoreCase);
  }
}
