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
    var (limit, window) = GetPolicy(featureType);
    var key = $"{userId}:{featureType}";
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
        return Task.FromResult((false, retryAfter));
      }

      state.Requests.Enqueue(now);
      return Task.FromResult((true, 0));
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
