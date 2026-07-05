#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using CleanArc.Application.Contracts.Achievements;
using CleanArc.Application.Contracts.Notifications;
using CleanArc.Domain.Entities.Achievement;
using CleanArc.Infrastructure.Persistence.Services.Achievements;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services;

internal sealed class AchievementTrackingService(
    ApplicationDbContext dbContext,
    IBackgroundJobManager backgroundJobManager)
    : IAchievementTrackingService
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
  };

  public async Task<IReadOnlyList<int>> TrackEventAsync(
      int userId,
      string eventType,
      string eventId,
      string propertiesJson,
      CancellationToken cancellationToken = default,
      string? timestamp = null)
  {
    return await ProcessEventThroughQueueAsync(userId, eventType, eventId, propertiesJson, timestamp, cancellationToken);
  }

  public async Task<IReadOnlyList<int>> ProcessEventThroughQueueAsync(
      int userId,
      string eventType,
      string eventId,
      string propertiesJson,
      string? timestamp = null,
      CancellationToken cancellationToken = default)
  {
    var normalizedEventType = AchievementEventTypeExtensions.NormalizeEventType(eventType);
    if (string.IsNullOrWhiteSpace(normalizedEventType))
      return Array.Empty<int>();

    // Validate that the event type is a known achievement event type
    if (!AchievementEventTypeExtensions.TryParseEventType(normalizedEventType, out var _))
    {
      System.Diagnostics.Debug.WriteLine($"Unknown achievement event type: {normalizedEventType}");
      return Array.Empty<int>();
    }

    var normalizedEventId = string.IsNullOrWhiteSpace(eventId)
        ? $"{normalizedEventType}:{Guid.NewGuid():N}"
        : eventId.Trim();

    var safePropertiesJson = string.IsNullOrWhiteSpace(propertiesJson) ? "{}" : propertiesJson;

    // Check processed events table first (idempotency check)
    var processedEvent = await dbContext.ProcessedEvents
        .AsNoTracking()
        .FirstOrDefaultAsync(pe => pe.Id == normalizedEventId, cancellationToken);

    if (processedEvent is not null)
    {
        return string.IsNullOrWhiteSpace(processedEvent.UnlockedBadgeIds)
            ? Array.Empty<int>()
            : processedEvent.UnlockedBadgeIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
    }

    // Write event to the database (UserAchievementEvents acting as inbox/history log)
    DateTime processedAt = DateTime.UtcNow;
    if (!string.IsNullOrWhiteSpace(timestamp) && DateTime.TryParse(timestamp, out var parsedTimestamp))
    {
        processedAt = parsedTimestamp.ToUniversalTime();
    }

    var inboxEvent = new UserAchievementEvent
    {
      UserId = userId,
      EventType = normalizedEventType,
      EventId = normalizedEventId,
      PropertiesJson = safePropertiesJson,
      ProcessedAt = processedAt,
    };

    dbContext.UserAchievementEvents.Add(inboxEvent);

    try
    {
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
      // Duplicate event: already processed in parallel. Fetch previously unlocked badges.
      var existingProcessed = await dbContext.ProcessedEvents
          .AsNoTracking()
          .FirstOrDefaultAsync(pe => pe.Id == normalizedEventId, cancellationToken);
      if (existingProcessed is not null)
      {
          return string.IsNullOrWhiteSpace(existingProcessed.UnlockedBadgeIds)
              ? Array.Empty<int>()
              : existingProcessed.UnlockedBadgeIds
                  .Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(int.Parse)
                  .ToList();
      }
      return Array.Empty<int>();
    }

    // Directly execute tracking job synchronously to eliminate Hangfire latency & polling database queries.
    return await ExecuteTrackingJobAsync(userId, normalizedEventType, normalizedEventId, safePropertiesJson, cancellationToken);
  }

  public async Task<IReadOnlyList<int>> ExecuteTrackingJobAsync(
      int userId,
      string eventType,
      string eventId,
      string propertiesJson,
      CancellationToken cancellationToken = default)
  {
    var normalizedEventType = AchievementEventTypeExtensions.NormalizeEventType(eventType) ?? eventType;
    var normalizedEventId = eventId.Trim();

    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    // Serialize by user within the transaction to avoid lost updates under concurrency.
    await dbContext.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT pg_advisory_xact_lock({userId})",
        cancellationToken);

    // Re-verify processed events inside the transaction to avoid race conditions.
    var processedEvent = await dbContext.ProcessedEvents
        .FirstOrDefaultAsync(pe => pe.Id == normalizedEventId, cancellationToken);

    if (processedEvent is not null)
    {
        await transaction.CommitAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(processedEvent.UnlockedBadgeIds)
            ? Array.Empty<int>()
            : processedEvent.UnlockedBadgeIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
    }

    var eventProperties = ParseJsonObject(propertiesJson);
    if (eventProperties is null)
      eventProperties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

    var activeBadgeIds = await dbContext.Badges
        .AsNoTracking()
        .Where(b => b.IsActive)
        .Select(b => b.Id)
        .ToListAsync(cancellationToken);
    var earnedBadgeIdList = await dbContext.UserBadges
        .AsNoTracking()
        .Where(ub => ub.UserId == userId)
        .Select(ub => ub.BadgeId)
        .ToListAsync(cancellationToken);

    var earnedBadgeIds = earnedBadgeIdList.ToHashSet();

    var candidateRules = new List<CandidateRule>();

    // 1) Preferred path: evaluate rules from AchievementTrigger table.
    var triggerRows = await dbContext.AchievementTriggers
        .AsNoTracking()
        .Where(t => t.IsActive && activeBadgeIds.Contains(t.BadgeId))
        .ToListAsync(cancellationToken);

    foreach (var trigger in triggerRows)
    {
      if (!AchievementEventTypeExtensions.EventTypeMatches(trigger.EventType, normalizedEventType))
        continue;

      if (earnedBadgeIds.Contains(trigger.BadgeId) && !trigger.IsRepeatable)
        continue;

      var filters = ParseFiltersJson(trigger.FilterConditionsJson);
      if (!MatchesFilters(filters, eventProperties))
        continue;

      candidateRules.Add(new CandidateRule
      {
        BadgeId = trigger.BadgeId,
        Aggregation = trigger.AggregationType,
        SourceField = trigger.AggregationSourceField,
        Threshold = trigger.Threshold,
        Filters = filters,
        Predicate = null,
      });
    }

    // 2) Backward-compatible fallback: RuleJson only for badges without triggers.
    var triggerBadgeIds = triggerRows.Select(t => t.BadgeId).ToHashSet();
    var fallbackBadges = await dbContext.Badges
        .AsNoTracking()
        .Where(b => b.IsActive && !string.IsNullOrWhiteSpace(b.RuleJson) && !triggerBadgeIds.Contains(b.Id))
        .ToListAsync(cancellationToken);

    foreach (var badge in fallbackBadges)
    {
      if (earnedBadgeIds.Contains(badge.Id))
        continue;

      var parsedRule = TryParseRule(badge.RuleJson!);
      if (parsedRule is null)
        continue;

      if (!AchievementEventTypeExtensions.EventTypeMatches(parsedRule.EventType, normalizedEventType))
        continue;

      if (!MatchesFilters(parsedRule.Filters, eventProperties))
        continue;

      if (!MatchesPredicate(parsedRule.Predicate, eventProperties))
        continue;

      candidateRules.Add(new CandidateRule
      {
        BadgeId = badge.Id,
        Aggregation = parsedRule.Aggregation,
        SourceField = parsedRule.SourceField,
        Threshold = parsedRule.Threshold,
        Filters = parsedRule.Filters,
        Predicate = parsedRule.Predicate,
      });
    }

    if (candidateRules.Count == 0)
    {
      var peEmpty = new ProcessedEvent(normalizedEventId)
      {
          ProcessedAt = DateTime.UtcNow,
          UnlockedBadgeIds = string.Empty
      };
      dbContext.ProcessedEvents.Add(peEmpty);
      await dbContext.SaveChangesAsync(cancellationToken);

      await transaction.CommitAsync(cancellationToken);
      return Array.Empty<int>();
    }

    var badgeIds = candidateRules.Select(x => x.BadgeId).Distinct().ToList();
    var progressMap = await dbContext.UserBadgeProgresses
        .Where(p => p.UserId == userId && badgeIds.Contains(p.BadgeId))
        .ToDictionaryAsync(p => p.BadgeId, cancellationToken);

    var unlockBadgeIds = new List<int>();
    var now = DateTime.UtcNow;

    foreach (var rule in candidateRules)
    {
      progressMap.TryGetValue(rule.BadgeId, out var progressRow);
      var current = progressRow?.ProgressValue ?? 0m;
      var updated = BadgeRuleEvaluator.ApplyAggregation(
          rule.Aggregation,
          current,
          eventProperties,
          rule.SourceField);

      if (updated < current)
        updated = current;

      if (progressRow is null)
      {
        progressRow = new UserBadgeProgress
        {
          UserId = userId,
          BadgeId = rule.BadgeId,
          ProgressValue = updated,
          LastEvaluatedAt = now,
        };
        dbContext.UserBadgeProgresses.Add(progressRow);
        progressMap[rule.BadgeId] = progressRow;
      }
      else
      {
        progressRow.ProgressValue = updated;
        progressRow.LastEvaluatedAt = now;
      }

      if (updated >= rule.Threshold)
      {
        unlockBadgeIds.Add(rule.BadgeId);
      }
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    var unlockedNow = await UnlockBadgesAsync(userId, unlockBadgeIds, now, cancellationToken);

    var newProcessedEvent = new ProcessedEvent(normalizedEventId)
    {
        ProcessedAt = DateTime.UtcNow,
        UnlockedBadgeIds = string.Join(",", unlockedNow)
    };
    dbContext.ProcessedEvents.Add(newProcessedEvent);
    await dbContext.SaveChangesAsync(cancellationToken);

    await transaction.CommitAsync(cancellationToken);
    return unlockedNow;
  }

  public async Task ReconcileAllUsersAchievementsAsync(CancellationToken cancellationToken = default)
  {
      var userIds = await dbContext.Users
          .AsNoTracking()
          .Select(u => u.Id)
          .ToListAsync(cancellationToken);

      foreach (var userId in userIds)
      {
          backgroundJobManager.EnqueueSyncStudentAchievements(userId);
      }
  }

  public async Task<IReadOnlyList<StudentAchievementDto>> GetStudentAchievementsAsync(
      int userId,
      CancellationToken cancellationToken = default)
  {
    var badges = await dbContext.Badges
        .AsNoTracking()
        .Where(b => b.IsActive)
        .OrderBy(b => b.Category)
        .ThenBy(b => b.Id)
        .ToListAsync(cancellationToken);

    var badgeIds = badges.Select(b => b.Id).ToList();

    var triggers = await dbContext.AchievementTriggers
        .AsNoTracking()
        .Where(t => t.IsActive && badgeIds.Contains(t.BadgeId))
        .OrderBy(t => t.EvaluationOrder)
        .ThenBy(t => t.Id)
        .ToListAsync(cancellationToken);

    var triggerMap = triggers
        .GroupBy(t => t.BadgeId)
        .ToDictionary(g => g.Key, g => g.First());

    var progressMap = await dbContext.UserBadgeProgresses
        .AsNoTracking()
        .Where(p => p.UserId == userId && badgeIds.Contains(p.BadgeId))
        .ToDictionaryAsync(p => p.BadgeId, p => p.ProgressValue, cancellationToken);

    var unlockMap = await dbContext.UserBadges
        .AsNoTracking()
        .Where(ub => ub.UserId == userId && badgeIds.Contains(ub.BadgeId))
        .ToDictionaryAsync(ub => ub.BadgeId, ub => ub.EarnedAt, cancellationToken);

    return badges.Select(badge =>
    {
      triggerMap.TryGetValue(badge.Id, out var trigger);
      progressMap.TryGetValue(badge.Id, out var progressValue);
      unlockMap.TryGetValue(badge.Id, out var unlockedAt);

      var target = trigger?.Threshold ?? ResolveRuleThreshold(badge.RuleJson);
      var isUnlocked = unlockMap.ContainsKey(badge.Id);
      var displayProgress = isUnlocked && target > 0 && progressValue < target
          ? target
          : progressValue;

      return new StudentAchievementDto(
        badge.Id,
        badge.Code,
        badge.Name,
        badge.Description,
        badge.Category,
        trigger?.EventType ?? ResolveRuleEventType(badge.RuleJson),
        displayProgress,
        target,
        isUnlocked,
        unlockedAt,
        badge.RewardXp,
        badge.RewardDiamonds,
        badge.ImageRef,
        badge.ImageRef);
    }).ToList();
  }

  public async Task<IReadOnlyList<int>> SyncStudentAchievementsAsync(
      int userId,
      CancellationToken cancellationToken = default)
  {
    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    await dbContext.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT pg_advisory_xact_lock({userId})",
        cancellationToken);

    var unlocked = new List<int>();

    // 1. Get all active badges
    var activeBadges = await dbContext.Badges
        .Where(b => b.IsActive)
        .ToListAsync(cancellationToken);

    // 2. For each active badge, parse its rule or trigger
    foreach (var badge in activeBadges)
    {
        // Skip if already earned and not repeatable
        var hasEarned = await dbContext.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badge.Id, cancellationToken);
        if (hasEarned) 
            continue; 

        decimal currentProgress = 0m;

        // Try parsing from AchievementTriggers
        var trigger = await dbContext.AchievementTriggers
            .FirstOrDefaultAsync(t => t.BadgeId == badge.Id && t.IsActive, cancellationToken);

        string? eventType = trigger?.EventType ?? ResolveRuleEventType(badge.RuleJson);
        string? aggType = trigger?.AggregationType ?? TryParseRule(badge.RuleJson!)?.Aggregation;
        string? srcField = trigger?.AggregationSourceField ?? TryParseRule(badge.RuleJson!)?.SourceField;

        if (string.IsNullOrEmpty(eventType))
        {
            // Fallback to hardcoded codes if no declarative rules exist
            if (badge.Code == "FIRST_CHALLENGE" || badge.Code == "COMPLETE_3_CHALLENGES" || badge.Code == "COMPLETE_10_CHALLENGES")
            {
                eventType = "CHALLENGE_COMPLETED";
                aggType = "count";
            }
            else if (badge.Code == "PERFECT_SCORE")
            {
                eventType = "QuizPerfectScore";
                aggType = "count";
            }
            else if (badge.Code == "REACH_LEVEL_5" || badge.Code == "REACH_LEVEL_10")
            {
                eventType = "LevelMilestone";
                aggType = "max";
            }
            else if (badge.Code == "OWN_3_MASCOTS")
            {
                eventType = "ItemPurchased";
                aggType = "count";
            }
            else if (badge.Code == "COMPLETE_1_MODULE")
            {
                eventType = "ModuleCompleted";
                aggType = "count";
            }
            else if (badge.Code == "TEAM_PLAYER")
            {
                eventType = "ClassroomJoined";
                aggType = "count";
            }
        }

        if (!string.IsNullOrEmpty(eventType))
        {
            currentProgress = await AggregateHistoryAsync(userId, eventType, aggType, srcField, cancellationToken);
            unlocked.AddRange(await SetProgressByCodeAsync(userId, badge.Code, currentProgress, cancellationToken));
        }
    }

    await transaction.CommitAsync(cancellationToken);
    return unlocked.Distinct().ToList();
  }

  private async Task<decimal> AggregateHistoryAsync(
      int userId,
      string eventType,
      string? aggregationType,
      string? sourceField,
      CancellationToken cancellationToken)
  {
      var normalizedEventType = AchievementEventTypeExtensions.NormalizeEventType(eventType) ?? eventType;
      var agg = (aggregationType ?? "count").Trim().ToLowerInvariant();

      // Aggregate state directly from database transaction histories:
      if (string.Equals(normalizedEventType, "ClassroomJoined", StringComparison.OrdinalIgnoreCase) || 
          string.Equals(normalizedEventType, "classroom_joined", StringComparison.OrdinalIgnoreCase))
      {
          return await dbContext.ClassroomStudents
              .AsNoTracking()
              .CountAsync(cs => cs.UserId == userId, cancellationToken);
      }
      else if (string.Equals(normalizedEventType, "CHALLENGE_COMPLETED", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedEventType, "QuizCompleted", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedEventType, "attempt_completed", StringComparison.OrdinalIgnoreCase))
      {
          return await dbContext.Attempts
              .AsNoTracking()
              .CountAsync(a => a.UserId == userId && a.IsCompleted, cancellationToken);
      }
      else if (string.Equals(normalizedEventType, "QuizPerfectScore", StringComparison.OrdinalIgnoreCase))
      {
          return await dbContext.Attempts
              .AsNoTracking()
              .CountAsync(a => a.UserId == userId && a.IsCompleted && a.StarsEarned >= 3, cancellationToken);
      }
      else if (string.Equals(normalizedEventType, "LevelMilestone", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedEventType, "LEVEL_REACHED", StringComparison.OrdinalIgnoreCase))
      {
          var level = await dbContext.UserProgresses
              .AsNoTracking()
              .Where(up => up.UserId == userId)
              .Select(up => (int?)up.CurrentLevel)
              .FirstOrDefaultAsync(cancellationToken);
          level ??= await dbContext.Users
              .AsNoTracking()
              .Where(u => u.Id == userId)
              .Select(u => (int?)u.Level)
              .FirstOrDefaultAsync(cancellationToken);
          return level ?? 1;
      }
      else if (string.Equals(normalizedEventType, "ItemPurchased", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedEventType, "MASCOT_PURCHASED", StringComparison.OrdinalIgnoreCase))
      {
          return await dbContext.UserInventoryItems
              .AsNoTracking()
              .Include(item => item.ShopItem)
              .Where(item => item.UserId == userId && item.ShopItem.Category.ToLower() == "avatar")
              .Select(item => item.ShopItemId)
              .Distinct()
              .CountAsync(cancellationToken);
      }
      else if (string.Equals(normalizedEventType, "ModuleCompleted", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedEventType, "MODULE_COMPLETED", StringComparison.OrdinalIgnoreCase))
      {
          return await CountCompletedModulesAsync(userId, cancellationToken);
      }
      else if (string.Equals(normalizedEventType, "diamond_earned", StringComparison.OrdinalIgnoreCase))
      {
          return await dbContext.DiamondTransactions
              .AsNoTracking()
              .Where(t => t.UserId == userId && t.Amount > 0)
              .SumAsync(t => (decimal)t.Amount, cancellationToken);
      }
      else if (string.Equals(normalizedEventType, "diamond_spent", StringComparison.OrdinalIgnoreCase))
      {
          return await dbContext.DiamondTransactions
              .AsNoTracking()
              .Where(t => t.UserId == userId && t.Amount < 0)
              .SumAsync(t => (decimal)-t.Amount, cancellationToken);
      }
      else if (string.Equals(normalizedEventType, "daily_check_in", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalizedEventType, "DailyStreakReached", StringComparison.OrdinalIgnoreCase))
      {
          return await dbContext.UserStreaks
              .AsNoTracking()
              .Where(us => us.UserId == userId)
              .Select(us => (decimal)us.CurrentStreak)
              .FirstOrDefaultAsync(cancellationToken);
      }

      // Default/fallback: check general UserAchievementEvents logged in history
      var query = dbContext.UserAchievementEvents
          .AsNoTracking()
          .Where(e => e.UserId == userId && e.EventType == normalizedEventType);

      if (agg == "count")
      {
          return await query.CountAsync(cancellationToken);
      }
      else if (agg == "sum" && !string.IsNullOrWhiteSpace(sourceField))
      {
          var events = await query.ToListAsync(cancellationToken);
          decimal sum = 0m;
          foreach (var ev in events)
          {
              var props = ParseJsonObject(ev.PropertiesJson);
              if (props != null && props.TryGetValue(sourceField, out var propVal))
              {
                  if (propVal.ValueKind == JsonValueKind.Number && propVal.TryGetDecimal(out var d))
                      sum += d;
              }
          }
          return sum;
      }
      else if (agg == "max" && !string.IsNullOrWhiteSpace(sourceField))
      {
          var events = await query.ToListAsync(cancellationToken);
          decimal max = 0m;
          foreach (var ev in events)
          {
              var props = ParseJsonObject(ev.PropertiesJson);
              if (props != null && props.TryGetValue(sourceField, out var propVal))
              {
                  if (propVal.ValueKind == JsonValueKind.Number && propVal.TryGetDecimal(out var d))
                      max = Math.Max(max, d);
              }
          }
          return max;
      }

      return 0m;
  }

  private async Task<IReadOnlyList<int>> SetProgressByCodeAsync(
      int userId,
      string badgeCode,
      decimal progressValue,
      CancellationToken cancellationToken)
  {
    var badge = await dbContext.Badges
        .FirstOrDefaultAsync(b => b.Code == badgeCode && b.IsActive, cancellationToken);

    if (badge is null)
      return Array.Empty<int>();

    var target = await dbContext.AchievementTriggers
        .AsNoTracking()
        .Where(t => t.BadgeId == badge.Id && t.IsActive)
        .OrderBy(t => t.EvaluationOrder)
        .ThenBy(t => t.Id)
        .Select(t => (decimal?)t.Threshold)
        .FirstOrDefaultAsync(cancellationToken)
        ?? ResolveRuleThreshold(badge.RuleJson);

    var now = DateTime.UtcNow;
    var existing = await dbContext.UserBadgeProgresses
        .FirstOrDefaultAsync(p => p.UserId == userId && p.BadgeId == badge.Id, cancellationToken);

    if (existing is null)
    {
      dbContext.UserBadgeProgresses.Add(new UserBadgeProgress
      {
        UserId = userId,
        BadgeId = badge.Id,
        ProgressValue = progressValue,
        LastEvaluatedAt = now,
      });
    }
    else
    {
      existing.ProgressValue = progressValue;
      existing.LastEvaluatedAt = now;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    if (target <= 0 || progressValue < target)
      return Array.Empty<int>();

    return await UnlockBadgesAsync(userId, new[] { badge.Id }, now, cancellationToken);
  }

  private async Task<IReadOnlyList<int>> UnlockBadgesAsync(
      int userId,
      IEnumerable<int> badgeIds,
      DateTime now,
      CancellationToken cancellationToken)
  {
    var unlockedNow = new List<int>();
    foreach (var badgeId in badgeIds.Distinct())
    {
      var rows = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO ""UserBadges"" (""UserId"", ""BadgeId"", ""EarnedAt"", ""IsFeatured"", ""SlotIndex"", ""CreatedTime"", ""ModifiedDate"")
VALUES ({userId}, {badgeId}, {now}, FALSE, NULL, {now}, {now})
ON CONFLICT (""UserId"", ""BadgeId"") DO NOTHING;", cancellationToken);

      if (rows > 0)
      {
        unlockedNow.Add(badgeId);
      }
    }

    if (unlockedNow.Count > 0)
    {
      await AwardBadgeRewardsAsync(userId, unlockedNow, cancellationToken);
    }

    return unlockedNow;
  }

  private async Task<int> CountCompletedModulesAsync(int userId, CancellationToken cancellationToken)
  {
    var progressRows = await dbContext.ChallengeProgresses
        .AsNoTracking()
        .Include(progress => progress.Challenge)
        .Where(progress =>
            progress.UserId == userId &&
            progress.HasCompleted &&
            progress.Challenge.ModuleId != null &&
            progress.Challenge.ClassroomId != null &&
            (progress.Challenge.SourceType == null || progress.Challenge.SourceType != "RECOVERY_MISSION"))
        .Select(progress => new
        {
          ClassroomId = progress.Challenge.ClassroomId!.Value,
          ModuleId = progress.Challenge.ModuleId!.Value,
        })
        .Distinct()
        .ToListAsync(cancellationToken);

    var completed = 0;
    foreach (var row in progressRows)
    {
      var total = await dbContext.Challenges.AsNoTracking()
          .CountAsync(challenge =>
              challenge.ClassroomId == row.ClassroomId &&
              challenge.ModuleId == row.ModuleId &&
              (challenge.SourceType == null || challenge.SourceType != "RECOVERY_MISSION"),
              cancellationToken);

      if (total == 0)
        continue;

      var completedInModule = await dbContext.ChallengeProgresses.AsNoTracking()
          .CountAsync(progress =>
              progress.UserId == userId &&
              progress.ClassroomId == row.ClassroomId &&
              progress.HasCompleted &&
              progress.Challenge.ModuleId == row.ModuleId,
              cancellationToken);

      if (completedInModule >= total)
        completed++;
    }

    return completed;
  }

  private static decimal ResolveRuleThreshold(string? ruleJson)
  {
    if (string.IsNullOrWhiteSpace(ruleJson))
      return 0m;

    try
    {
      using var doc = JsonDocument.Parse(ruleJson);
      var root = doc.RootElement;
      JsonElement target = root;
      if (root.TryGetProperty("rule", out var ruleProp) && ruleProp.ValueKind == JsonValueKind.Object)
      {
          target = ruleProp;
      }

      return target.TryGetProperty("threshold", out var threshold)
          && threshold.ValueKind == JsonValueKind.Number
          && threshold.TryGetDecimal(out var value)
        ? value
        : 0m;
    }
    catch
    {
      return 0m;
    }
  }

  private static string? ResolveRuleEventType(string? ruleJson)
  {
    if (string.IsNullOrWhiteSpace(ruleJson))
      return null;

    try
    {
      using var doc = JsonDocument.Parse(ruleJson);
      var root = doc.RootElement;
      JsonElement target = root;
      if (root.TryGetProperty("rule", out var ruleProp) && ruleProp.ValueKind == JsonValueKind.Object)
      {
          target = ruleProp;
      }

      if (target.TryGetProperty("triggerEvent", out var triggerEvent) && triggerEvent.ValueKind == JsonValueKind.String)
      {
          return triggerEvent.GetString();
      }

      return target.TryGetProperty("eventType", out var eventType)
          && eventType.ValueKind == JsonValueKind.String
        ? eventType.GetString()
        : null;
    }
    catch
    {
      return null;
    }
  }

  private async Task AwardBadgeRewardsAsync(
      int userId,
      IReadOnlyCollection<int> unlockedBadgeIds,
      CancellationToken cancellationToken)
  {
    var rewardBadges = await dbContext.Badges
      .Where(b => unlockedBadgeIds.Contains(b.Id) && (b.RewardXp > 0 || b.RewardDiamonds > 0 || b.RewardDreamTokens > 0))
        .ToListAsync(cancellationToken);

    if (rewardBadges.Count == 0)
      return;

    var totalXp = rewardBadges.Sum(b => b.RewardXp);
    var totalDiamonds = rewardBadges.Sum(b => b.RewardDiamonds);
    var totalDreamTokens = rewardBadges.Sum(b => b.RewardDreamTokens);

    if (totalXp <= 0 && totalDiamonds <= 0 && totalDreamTokens <= 0)
      return;

    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    if (user is null)
      return;

    if (totalXp > 0)
    {
      user.Experience += totalXp;

      var progress = await dbContext.UserProgresses.FirstOrDefaultAsync(
          up => up.UserId == userId,
          cancellationToken);

      if (progress is null)
      {
        progress = new Domain.Entities.Progression.UserProgress
        {
          UserId = userId,
          TotalXP = totalXp,
          CurrentLevel = 1,
          TotalQuizzesTaken = 0,
          TotalCorrectAnswers = 0,
          TotalTimePlayed = 0,
        };
        dbContext.UserProgresses.Add(progress);
      }
      else
      {
        progress.TotalXP += totalXp;
      }

      var eligibleLevel = await dbContext.Levels
          .AsNoTracking()
          .Where(level => level.RequiredXP <= progress.TotalXP)
          .OrderByDescending(level => level.LevelNumber)
          .FirstOrDefaultAsync(cancellationToken);

      if (eligibleLevel is not null && eligibleLevel.LevelNumber > progress.CurrentLevel)
      {
        progress.CurrentLevel = eligibleLevel.LevelNumber;
        user.Level = eligibleLevel.LevelNumber;
      }
    }

    if (totalDiamonds > 0)
    {
      user.Diamonds += totalDiamonds;
      dbContext.DiamondTransactions.Add(new Domain.Entities.Shop.DiamondTransaction
      {
        UserId = userId,
        Amount = totalDiamonds,
        Reason = "achievement_reward",
        ReferenceId = string.Join(",", unlockedBadgeIds),
      });
    }

    if (totalDreamTokens > 0)
    {
      user.DreamTokensCount += totalDreamTokens;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static BadgeRule? TryParseRule(string ruleJson)
  {
    try
    {
      using var doc = JsonDocument.Parse(ruleJson);
      var root = doc.RootElement;
      
      JsonElement targetElement = root;
      if (root.TryGetProperty("rule", out var ruleProp) && ruleProp.ValueKind == JsonValueKind.Object)
      {
          targetElement = ruleProp;
      }

      var rule = JsonSerializer.Deserialize<BadgeRule>(targetElement.GetRawText(), JsonOptions);
      if (rule != null)
      {
          if (targetElement.TryGetProperty("triggerEvent", out var triggerProp) && triggerProp.ValueKind == JsonValueKind.String)
          {
              rule.EventType = triggerProp.GetString() ?? string.Empty;
          }
          else if (string.IsNullOrEmpty(rule.EventType) && !string.IsNullOrEmpty(rule.TriggerEvent))
          {
              rule.EventType = rule.TriggerEvent;
          }
          return rule;
      }
      return null;
    }
    catch
    {
      return null;
    }
  }

  private static Dictionary<string, JsonElement>? ParseJsonObject(string json)
  {
    try
    {
      using var doc = JsonDocument.Parse(json);
      if (doc.RootElement.ValueKind != JsonValueKind.Object)
        return null;

      var map = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
      foreach (var prop in doc.RootElement.EnumerateObject())
      {
        map[prop.Name] = prop.Value.Clone();
      }

      return map;
    }
    catch
    {
      return null;
    }
  }

  private static Dictionary<string, JsonElement>? ParseFiltersJson(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
      return null;

    return ParseJsonObject(json);
  }

  private static bool MatchesFilters(
      Dictionary<string, JsonElement>? filters,
      Dictionary<string, JsonElement> props)
  {
    return BadgeRuleEvaluator.EvaluateFilters(filters, props);
  }

  private static bool MatchesPredicate(BadgeRulePredicate? predicate, Dictionary<string, JsonElement> props)
  {
    if (predicate is null)
      return true;

    var normalizedOperator = string.Equals(predicate.Operator, "neq", StringComparison.OrdinalIgnoreCase)
      ? "ne"
      : predicate.Operator;

    return BadgeRuleEvaluator.EvaluatePredicate(
      new CleanArc.Infrastructure.Persistence.Services.Achievements.BadgeRulePredicate
      {
        Field = predicate.Field,
        Operator = normalizedOperator,
        Value = predicate.Value,
      },
      props);
  }

  private sealed class BadgeRule
  {
    [JsonPropertyName("triggerEvent")]
    public string? TriggerEvent { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string Aggregation { get; set; } = "count";
    public decimal Threshold { get; set; }
    public string? SourceField { get; set; }
    public Dictionary<string, JsonElement>? Filters { get; set; }
    public BadgeRulePredicate? Predicate { get; set; }
  }

  private sealed class BadgeRulePredicate
  {
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "eq";
    public JsonElement Value { get; set; }
  }

  private sealed class CandidateRule
  {
    public int BadgeId { get; set; }
    public string Aggregation { get; set; } = "count";
    public decimal Threshold { get; set; }
    public string? SourceField { get; set; }
    public Dictionary<string, JsonElement>? Filters { get; set; }
    public BadgeRulePredicate? Predicate { get; set; }
  }

}
