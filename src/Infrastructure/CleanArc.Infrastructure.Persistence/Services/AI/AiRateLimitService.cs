using System.Collections.Concurrent;
using CleanArc.Application.Contracts.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.AI;

public sealed class AiRateLimitService(IOptions<AiRateLimitOptions> options) : IAiRateLimitService
{
  private readonly AiRateLimitOptions _options = options.Value;
  private readonly ConcurrentDictionary<string, WindowState> _windows = new();

  public Task<(bool Allowed, int RetryAfterSeconds)> TryAcquireAsync(
    int userId,
    string featureType,
    CancellationToken cancellationToken)
  {
    // Global per-user limiter across all AI features. This ensures the client can't
    // spam multiple AI endpoints rapidly (e.g., double taps) even if the per-feature
    // policy would allow it.
    var global = TryAcquireWindow($"{userId}:GLOBAL", _options.GlobalMaxRequests, TimeSpan.FromSeconds(_options.GlobalWindowSeconds));
    if (!global.Allowed)
      return Task.FromResult(global);

    var (limit, window) = GetPolicy(featureType);
    var featureResult = TryAcquireWindow($"{userId}:{featureType}", limit, window);
    return Task.FromResult(featureResult);
  }

  private (bool Allowed, int RetryAfterSeconds) TryAcquireWindow(string key, int limit, TimeSpan window)
  {
    // Fail open if misconfigured.
    if (limit <= 0)
      return (true, 0);

    if (window <= TimeSpan.Zero)
      return (true, 0);

    var now = DateTimeOffset.UtcNow;
    var state = _windows.GetOrAdd(key, _ => new WindowState());

    lock (state)
    {
      while (state.Requests.Count > 0 && now - state.Requests.Peek() > window)
      {
        state.Requests.Dequeue();
      }

      if (state.Requests.Count >= limit)
      {
        var oldest = state.Requests.Peek();
        var retryAfter = Math.Max(1, (int)Math.Ceiling((window - (now - oldest)).TotalSeconds));
        return (false, retryAfter);
      }

      state.Requests.Enqueue(now);
      return (true, 0);
    }
  }

  private (int Limit, TimeSpan Window) GetPolicy(string featureType)
  {
    if (string.Equals(featureType, AiFeatureTypes.AdminAuditor, StringComparison.OrdinalIgnoreCase))
      return (_options.AuditorMaxRequests, TimeSpan.FromSeconds(_options.AuditorWindowSeconds));

    if (string.Equals(featureType, AiFeatureTypes.ClassroomThumbnailGeneration, StringComparison.OrdinalIgnoreCase)
      || string.Equals(featureType, AiFeatureTypes.StickerGeneration, StringComparison.OrdinalIgnoreCase))
    {
      return (_options.ImageMaxRequests, TimeSpan.FromMinutes(_options.ImageWindowMinutes));
    }

    return (_options.TextMaxRequests, TimeSpan.FromSeconds(_options.TextWindowSeconds));
  }

  private sealed class WindowState
  {
    public Queue<DateTimeOffset> Requests { get; } = new();
  }
}
