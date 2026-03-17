#nullable enable

using System.Text.Json;

namespace CleanArc.Infrastructure.Persistence.Services.Achievements;

/// <summary>
/// Evaluates and aggregates badge rule conditions.
/// Handles different aggregation strategies: count, sum, max.
/// Replaces inline switch statements in AchievementTrackingService for better maintainability.
/// </summary>
internal sealed class BadgeRuleEvaluator
{
  /// <summary>
  /// Strategy pattern for aggregation types
  /// </summary>
  private static readonly Dictionary<string, Func<decimal, Dictionary<string, JsonElement>, string?, decimal>>
    AggregationStrategies = new(StringComparer.OrdinalIgnoreCase)
    {
      ["count"] = (current, _, __) => current + 1m,
      ["sum"] = (current, props, sourceField) =>
        current + ExtractDecimalValue(props, sourceField),
      ["max"] = (current, props, sourceField) =>
        Math.Max(current, ExtractDecimalValue(props, sourceField)),
      ["current_streak"] = (_, props, sourceField) =>
        ExtractDecimalValue(props, string.IsNullOrWhiteSpace(sourceField) ? "currentStreak" : sourceField),
    };

  /// <summary>
  /// Applies aggregation logic to calculate new progress value.
  /// </summary>
  public static decimal ApplyAggregation(
    string aggregationType,
    decimal currentProgress,
    Dictionary<string, JsonElement> eventProperties,
    string? sourceField)
  {
    var normalizedType = (aggregationType ?? "count").Trim().ToLowerInvariant();

    if (!AggregationStrategies.TryGetValue(normalizedType, out var strategy))
    {
      // Unknown aggregation type, default to count
      return currentProgress + 1m;
    }

    try
    {
      var result = strategy(currentProgress, eventProperties, sourceField);
      if (string.Equals(normalizedType, "current_streak", StringComparison.OrdinalIgnoreCase))
      {
        return Math.Max(0m, result);
      }

      // Ensure progress never goes backwards
      return Math.Max(result, currentProgress);
    }
    catch
    {
      // If evaluation fails, return current progress unchanged
      return currentProgress;
    }
  }

  /// <summary>
  /// Checks if an event meets filter conditions.
  /// All filters must match for this to return true.
  /// </summary>
  public static bool EvaluateFilters(
    Dictionary<string, JsonElement>? filters,
    Dictionary<string, JsonElement> eventProperties)
  {
    if (filters is null || filters.Count == 0)
      return true; // No filters means all events pass

    foreach (var filterKv in filters)
    {
      if (!eventProperties.TryGetValue(filterKv.Key, out var eventValue))
        return false; // Required filter field not in event

      var filterValue = filterKv.Value;
      if (filterValue.ValueKind == JsonValueKind.Object
          && filterValue.TryGetProperty("operator", out var opElement)
          && opElement.ValueKind == JsonValueKind.String
          && filterValue.TryGetProperty("value", out var compareValue))
      {
        var op = opElement.GetString() ?? "eq";
        if (!CompareValues(eventValue, compareValue, op))
          return false;

        continue;
      }

      if (!JsonElementsEqual(eventValue, filterValue))
        return false; // Filter value doesn't match event value
    }

    return true;
  }

  /// <summary>
  /// Evaluates a predicate condition (e.g., score > 80, level == 5).
  /// </summary>
  public static bool EvaluatePredicate(
    BadgeRulePredicate? predicate,
    Dictionary<string, JsonElement> eventProperties)
  {
    if (predicate is null || string.IsNullOrWhiteSpace(predicate.Field))
      return true; // No predicate means condition passes

    if (!eventProperties.TryGetValue(predicate.Field, out var eventValue))
      return false; // Field not in event

    var op = (predicate.Operator ?? "eq").Trim().ToLowerInvariant();
    return CompareValues(eventValue, predicate.Value, op);
  }

  /// <summary>
  /// Compares two JSON elements for equality or inequality.
  /// </summary>
  private static bool JsonElementsEqual(JsonElement left, JsonElement right)
  {
    if (left.ValueKind != right.ValueKind)
      return false;

    return left.ValueKind switch
    {
      JsonValueKind.String => left.GetString() == right.GetString(),
      JsonValueKind.Number =>
        left.TryGetDecimal(out var leftDecimal)
        && right.TryGetDecimal(out var rightDecimal)
        && leftDecimal == rightDecimal,
      JsonValueKind.True or JsonValueKind.False =>
        left.GetBoolean() == right.GetBoolean(),
      JsonValueKind.Null => true,
      _ => left.GetRawText() == right.GetRawText(),
    };
  }

  /// <summary>
  /// Compares values using various operators.
  /// </summary>
  private static bool CompareValues(JsonElement value, JsonElement compareValue, string op)
  {
    return op switch
    {
      "eq" => JsonElementsEqual(value, compareValue),
      "ne" => !JsonElementsEqual(value, compareValue),
      "gt" => TryCompareNumeric(value, compareValue, x => x > 0),
      "gte" => TryCompareNumeric(value, compareValue, x => x >= 0),
      "lt" => TryCompareNumeric(value, compareValue, x => x < 0),
      "lte" => TryCompareNumeric(value, compareValue, x => x <= 0),
      "contains" => TryCompareString(value, compareValue, (s, c) => s.Contains(c)),
      "startsWith" =>
        TryCompareString(value, compareValue, (s, c) => s.StartsWith(c)),
      "endsWith" => TryCompareString(value, compareValue, (s, c) => s.EndsWith(c)),
      _ => false, // Unknown operator
    };
  }

  /// <summary>
  /// Extracts a decimal value from event properties.
  /// </summary>
  private static decimal ExtractDecimalValue(
    Dictionary<string, JsonElement> props,
    string? fieldName)
  {
    if (string.IsNullOrWhiteSpace(fieldName) || !props.TryGetValue(fieldName, out var element))
      return 0m;

    if (element.TryGetDecimal(out var value))
      return value;

    if (element.TryGetInt32(out var intValue))
      return intValue;

    if (element.TryGetInt64(out var longValue))
      return longValue;

    return 0m;
  }

  /// <summary>
  /// Compares numeric values using a comparison function.
  /// </summary>
  private static bool TryCompareNumeric(
    JsonElement value,
    JsonElement compareValue,
    Func<int, bool> compareFn)
  {
    if (!value.TryGetDecimal(out var valDecimal))
      return false;
    if (!compareValue.TryGetDecimal(out var compDecimal))
      return false;

    var cmp = valDecimal.CompareTo(compDecimal);
    return compareFn(cmp);
  }

  /// <summary>
  /// Compares string values using a comparison function.
  /// </summary>
  private static bool TryCompareString(
    JsonElement value,
    JsonElement compareValue,
    Func<string, string, bool> compareFn)
  {
    var valueStr = value.GetString();
    var compareStr = compareValue.GetString();

    if (valueStr == null || compareStr == null)
      return false;

    return compareFn(valueStr, compareStr);
  }
}

/// <summary>
/// Represents a badge rule with conditions for evaluating achievement events.
/// </summary>
internal sealed class BadgeRule
{
  public string EventType { get; set; } = string.Empty;
  public string Aggregation { get; set; } = "count";
  public decimal Threshold { get; set; }
  public string? SourceField { get; set; }
  public Dictionary<string, JsonElement>? Filters { get; set; }
  public BadgeRulePredicate? Predicate { get; set; }
}

/// <summary>
/// Represents a predicate condition for evaluating event properties.
/// </summary>
internal sealed class BadgeRulePredicate
{
  public string Field { get; set; } = string.Empty;
  public string Operator { get; set; } = "eq";
  public JsonElement Value { get; set; }
}
