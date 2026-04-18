#nullable enable

using System.Text.Json;
using CleanArc.Application.Contracts.Achievements;
using CleanArc.Domain.Entities.Achievement;
using CleanArc.Infrastructure.Persistence.Services.Achievements;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services;

internal sealed class AchievementTrackingService(ApplicationDbContext dbContext)
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
      CancellationToken cancellationToken = default)
  {
    var normalizedEventType = AchievementEventTypeExtensions.NormalizeEventType(eventType);
    if (string.IsNullOrWhiteSpace(normalizedEventType))
      return Array.Empty<int>();

    // Validate that the event type is a known achievement event type
    if (!AchievementEventTypeExtensions.TryParseEventType(normalizedEventType, out var _))
    {
      // Log invalid event type (could also throw or return empty)
      System.Diagnostics.Debug.WriteLine($"Unknown achievement event type: {normalizedEventType}");
      return Array.Empty<int>();
    }

    var normalizedEventId = string.IsNullOrWhiteSpace(eventId)
        ? $"{normalizedEventType}:{Guid.NewGuid():N}"
        : eventId.Trim();

    var safePropertiesJson = string.IsNullOrWhiteSpace(propertiesJson) ? "{}" : propertiesJson;
    var eventProperties = ParseJsonObject(safePropertiesJson);
    if (eventProperties is null)
      eventProperties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    // Serialize by user within the transaction to avoid lost updates under concurrency.
    await dbContext.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT pg_advisory_xact_lock({userId})",
        cancellationToken);

    var inboxEvent = new UserAchievementEvent
    {
      UserId = userId,
      EventType = normalizedEventType,
      EventId = normalizedEventId,
      PropertiesJson = safePropertiesJson,
      ProcessedAt = DateTime.UtcNow,
    };

    dbContext.UserAchievementEvents.Add(inboxEvent);

    try
    {
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
      // Duplicate event: already processed.
      await transaction.RollbackAsync(cancellationToken);
      return Array.Empty<int>();
    }

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
        .Where(t => t.IsActive)
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
        .Where(b => !string.IsNullOrWhiteSpace(b.RuleJson) && !triggerBadgeIds.Contains(b.Id))
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

    var unlockedNow = new List<int>();
    foreach (var badgeId in unlockBadgeIds.Distinct())
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

    await transaction.CommitAsync(cancellationToken);
    return unlockedNow;
  }

  private async Task AwardBadgeRewardsAsync(
      int userId,
      IReadOnlyCollection<int> unlockedBadgeIds,
      CancellationToken cancellationToken)
  {
    var rewardBadges = await dbContext.Badges
        .Where(b => unlockedBadgeIds.Contains(b.Id) && (b.RewardXp > 0 || b.RewardDiamonds > 0))
        .ToListAsync(cancellationToken);

    if (rewardBadges.Count == 0)
      return;

    var totalXp = rewardBadges.Sum(b => b.RewardXp);
    var totalDiamonds = rewardBadges.Sum(b => b.RewardDiamonds);

    if (totalXp <= 0 && totalDiamonds <= 0)
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
      }
    }

    if (totalDiamonds > 0)
    {
      user.Diamonds += totalDiamonds;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static BadgeRule? TryParseRule(string ruleJson)
  {
    try
    {
      return JsonSerializer.Deserialize<BadgeRule>(ruleJson, JsonOptions);
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
