#nullable enable

using System.Text.Json;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Achievements.Queries;

internal class GetUserBadgeProgressQueryHandler : IRequestHandler<GetUserBadgeProgressQuery, OperationResult<List<UserBadgeProgressDto>>>
{
  private sealed class BadgeProgressDefinition
  {
    public decimal Target { get; set; }
    public string? AggregationType { get; set; }
    public string? AggregationSourceField { get; set; }
  }

  private readonly IUnitOfWork _unitOfWork;

  public GetUserBadgeProgressQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<UserBadgeProgressDto>>> Handle(
      GetUserBadgeProgressQuery request,
      CancellationToken cancellationToken)
  {
    var badges = await _unitOfWork.BadgeRepository.GetAllBadgesAsync();
    var triggers = await _unitOfWork.BadgeRepository.GetAchievementTriggersAsync();
    var progressRows = await _unitOfWork.BadgeRepository.GetUserBadgeProgressesAsync(request.UserId);
    var userBadges = await _unitOfWork.BadgeRepository.GetUserBadgesAsync(request.UserId);

    var unlockedSet = userBadges.Select(ub => ub.BadgeId).ToHashSet();
    var progressMap = progressRows.ToDictionary(p => p.BadgeId, p => p.ProgressValue);

    var triggerDefinitionMap = triggers
        .GroupBy(t => t.BadgeId)
        .ToDictionary(
            g => g.Key,
            g =>
            {
              var selected = g.OrderBy(t => t.EvaluationOrder).ThenBy(t => t.Id).First();
              return new BadgeProgressDefinition
              {
                Target = selected.Threshold,
                AggregationType = selected.AggregationType,
                AggregationSourceField = selected.AggregationSourceField,
              };
            });

    var result = badges.Select(badge =>
    {
      var current = progressMap.TryGetValue(badge.Id, out var val) ? val : 0m;
      var progressDefinition = ResolveProgressDefinition(
        badge.Id,
        badge.RuleJson,
        triggerDefinitionMap);

      if (string.Equals(progressDefinition.AggregationType, "current_streak", StringComparison.OrdinalIgnoreCase)
        && current < 0)
      {
        current = 0m;
      }

      var target = progressDefinition.Target;
      var isUnlocked = unlockedSet.Contains(badge.Id);

      return new UserBadgeProgressDto(
          badge.Id,
          current,
          target,
          isUnlocked,
          progressDefinition.AggregationType,
          progressDefinition.AggregationSourceField,
          ResolveProgressLabel(progressDefinition.AggregationType, progressDefinition.AggregationSourceField));
    }).ToList();

    return OperationResult<List<UserBadgeProgressDto>>.SuccessResult(result);
  }

  private static BadgeProgressDefinition ResolveProgressDefinition(
      int badgeId,
      string? ruleJson,
      Dictionary<int, BadgeProgressDefinition> triggerDefinitionMap)
  {
    if (triggerDefinitionMap.TryGetValue(badgeId, out var definitionFromTrigger))
      return definitionFromTrigger;

    if (string.IsNullOrWhiteSpace(ruleJson))
      return new BadgeProgressDefinition { Target = 0m, AggregationType = "count" };

    try
    {
      using var doc = JsonDocument.Parse(ruleJson);
      var root = doc.RootElement;
      if (root.ValueKind == JsonValueKind.Object
          && root.TryGetProperty("threshold", out var thresholdElement)
          && thresholdElement.TryGetDecimal(out var threshold))
      {
        return new BadgeProgressDefinition
        {
          Target = threshold,
          AggregationType = root.TryGetProperty("aggregation", out var agg) ? agg.GetString() : "count",
          AggregationSourceField = root.TryGetProperty("sourceField", out var sourceField)
            ? sourceField.GetString()
            : null,
        };
      }
    }
    catch
    {
      // fallback below
    }

    return new BadgeProgressDefinition { Target = 0m, AggregationType = "count" };
  }

  private static string ResolveProgressLabel(string? aggregationType, string? sourceField)
  {
    var normalizedType = (aggregationType ?? "count").Trim().ToLowerInvariant();
    var normalizedSource = (sourceField ?? string.Empty).Trim().ToLowerInvariant();

    if (normalizedType == "current_streak")
      return "Current Streak";

    if (normalizedType == "sum" && normalizedSource.Contains("diamond"))
      return "Diamonds";

    if (normalizedType == "sum")
      return "Total Progress";

    if (normalizedType == "max")
      return "Best Progress";

    return "Progress";
  }
}
