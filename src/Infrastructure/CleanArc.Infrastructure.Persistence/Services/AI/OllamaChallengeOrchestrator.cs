using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.AI;

public class OllamaChallengeOrchestrator(
  HttpClient httpClient,
  IOptions<OllamaChallengeOptions> options)
  : ITextEmbeddingService
{
  private const int LocalEmbeddingDimensions = 256;
  private const int EmbedNotFoundThreshold = 2;

  private static int _consecutiveEmbedNotFound;
  private static int _remoteEmbedDisabled;

  private readonly HttpClient _httpClient = httpClient;
  private readonly OllamaChallengeOptions _options = options.Value;

  public async Task<OperationResult<float[]>> EmbedAsync(string text, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(text))
      return OperationResult<float[]>.FailureResult("Cannot embed empty text.");

    if (IsRemoteEmbedDisabled())
      return OperationResult<float[]>.SuccessResult(BuildLocalHashEmbedding(text));

    foreach (var attempt in BuildEmbedAttempts(text))
    {
      var attemptResult = await TryEmbedEndpointAsync(attempt, cancellationToken);
      if (attemptResult.Result.IsSuccess)
      {
        ResetEmbedNotFoundStreak();
        return attemptResult.Result;
      }

      if (attemptResult.IsNotFound)
      {
        RegisterEmbedNotFound();
        if (IsRemoteEmbedDisabled())
          break;

        continue;
      }

      ResetEmbedNotFoundStreak();
    }

    return OperationResult<float[]>.SuccessResult(BuildLocalHashEmbedding(text));
  }

  private async Task<EmbeddingAttemptResponse> TryEmbedEndpointAsync(EmbeddingAttempt attempt, CancellationToken cancellationToken)
  {
    try
    {
      using var response = await _httpClient.PostAsJsonAsync(attempt.Endpoint, attempt.Payload, cancellationToken);
      var body = await response.Content.ReadAsStringAsync(cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        var result = OperationResult<float[]>.FailureResult(
          $"Ollama embedding failed at '{attempt.Endpoint}' ({(int)response.StatusCode}): {body}");
        return new EmbeddingAttemptResponse(result, response.StatusCode == HttpStatusCode.NotFound);
      }

      if (!TryExtractEmbeddingVector(body, out var vector))
      {
        var result = OperationResult<float[]>.FailureResult(
          $"Embedding response from '{attempt.Endpoint}' did not contain a usable vector.");
        return new EmbeddingAttemptResponse(result, false);
      }

      return new EmbeddingAttemptResponse(OperationResult<float[]>.SuccessResult(vector), false);
    }
    catch (Exception ex)
    {
      var result = OperationResult<float[]>.FailureResult(
        $"Ollama embedding request failed at '{attempt.Endpoint}': {ex.Message}");
      return new EmbeddingAttemptResponse(result, false);
    }
  }

  private static bool IsRemoteEmbedDisabled()
    => Volatile.Read(ref _remoteEmbedDisabled) == 1;

  private static void RegisterEmbedNotFound()
  {
    var streak = Interlocked.Increment(ref _consecutiveEmbedNotFound);
    if (streak >= EmbedNotFoundThreshold)
      Interlocked.Exchange(ref _remoteEmbedDisabled, 1);
  }

  private static void ResetEmbedNotFoundStreak()
    => Interlocked.Exchange(ref _consecutiveEmbedNotFound, 0);

  private IEnumerable<EmbeddingAttempt> BuildEmbedAttempts(string text)
  {
    var attempts = new List<EmbeddingAttempt>();
    var seenEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    AddEmbeddingAttempt(attempts, seenEndpoints, _options.EmbedEndpoint, text);
    AddEmbeddingAttempt(attempts, seenEndpoints, "/api/embed", text);
    AddEmbeddingAttempt(attempts, seenEndpoints, _options.EmbedFallbackEndpoint, text);
    AddEmbeddingAttempt(attempts, seenEndpoints, "/api/embeddings", text);

    return attempts;
  }

  private void AddEmbeddingAttempt(
    ICollection<EmbeddingAttempt> attempts,
    ISet<string> seenEndpoints,
    string endpoint,
    string text)
  {
    var normalizedEndpoint = NormalizeEndpoint(endpoint);
    if (!seenEndpoints.Add(normalizedEndpoint))
      return;

    var payload = BuildEmbeddingPayload(normalizedEndpoint, text);
    attempts.Add(new EmbeddingAttempt(normalizedEndpoint, payload));
  }

  private object BuildEmbeddingPayload(string endpoint, string text)
  {
    if (endpoint.Equals("/api/embeddings", StringComparison.OrdinalIgnoreCase))
    {
      return new
      {
        model = _options.EmbeddingModel,
        prompt = text,
      };
    }

    return new
    {
      model = _options.EmbeddingModel,
      input = text,
    };
  }

  private static bool TryExtractEmbeddingVector(string body, out float[] vector)
  {
    vector = [];

    try
    {
      using var doc = JsonDocument.Parse(body);

      if (doc.RootElement.TryGetProperty("embeddings", out var embeddingsElement)
          && embeddingsElement.ValueKind == JsonValueKind.Array
          && embeddingsElement.GetArrayLength() > 0)
      {
        var firstItem = embeddingsElement[0];
        if (firstItem.ValueKind == JsonValueKind.Array)
        {
          var nestedVector = firstItem.EnumerateArray()
            .Where(number => number.ValueKind == JsonValueKind.Number)
            .Select(ReadFloat)
            .ToArray();

          if (nestedVector.Length > 0)
          {
            vector = nestedVector;
            return true;
          }
        }

        if (firstItem.ValueKind == JsonValueKind.Number)
        {
          var flatVector = embeddingsElement.EnumerateArray()
            .Where(number => number.ValueKind == JsonValueKind.Number)
            .Select(ReadFloat)
            .ToArray();

          if (flatVector.Length > 0)
          {
            vector = flatVector;
            return true;
          }
        }
      }

      if (doc.RootElement.TryGetProperty("embedding", out var embeddingElement)
          && embeddingElement.ValueKind == JsonValueKind.Array)
      {
        var singleVector = embeddingElement.EnumerateArray()
          .Where(number => number.ValueKind == JsonValueKind.Number)
          .Select(ReadFloat)
          .ToArray();

        if (singleVector.Length > 0)
        {
          vector = singleVector;
          return true;
        }
      }

      return false;
    }
    catch
    {
      return false;
    }
  }

  private static float[] BuildLocalHashEmbedding(string text)
  {
    var vector = new float[LocalEmbeddingDimensions];
    var tokens = TokenizeForLocalEmbedding(text);

    if (tokens.Count == 0)
      tokens.Add(text.Trim().ToLowerInvariant());

    for (var index = 0; index < tokens.Count; index += 1)
    {
      var token = tokens[index];
      var primaryHash = Fnv1aHash(token);
      var secondaryHash = Fnv1aHash($"{token}|{index}");

      AccumulateHashedValue(vector, primaryHash, 1.0f);
      AccumulateHashedValue(vector, secondaryHash, 0.5f);
    }

    NormalizeVector(vector);
    return vector;
  }

  private static List<string> TokenizeForLocalEmbedding(string text)
  {
    var normalized = text.Trim().ToLowerInvariant();
    var tokens = new List<string>();
    var buffer = new StringBuilder();

    foreach (var character in normalized)
    {
      if (char.IsLetterOrDigit(character))
      {
        buffer.Append(character);
        continue;
      }

      if (buffer.Length == 0)
        continue;

      tokens.Add(buffer.ToString());
      buffer.Clear();
    }

    if (buffer.Length > 0)
      tokens.Add(buffer.ToString());

    return tokens;
  }

  private static uint Fnv1aHash(string value)
  {
    const uint offsetBasis = 2166136261;
    const uint prime = 16777619;

    var hash = offsetBasis;
    foreach (var character in value)
    {
      hash ^= character;
      hash *= prime;
    }

    return hash;
  }

  private static void AccumulateHashedValue(float[] vector, uint hash, float magnitude)
  {
    var slot = (int)(hash % LocalEmbeddingDimensions);
    var sign = (hash & 1u) == 0u ? 1.0f : -1.0f;
    vector[slot] += sign * magnitude;
  }

  private static void NormalizeVector(float[] vector)
  {
    double sumSquares = 0;
    for (var i = 0; i < vector.Length; i += 1)
      sumSquares += vector[i] * vector[i];

    if (sumSquares <= 0)
      return;

    var scale = 1.0 / Math.Sqrt(sumSquares);
    for (var i = 0; i < vector.Length; i += 1)
      vector[i] = (float)(vector[i] * scale);
  }

  private static float ReadFloat(JsonElement number)
  {
    if (number.TryGetSingle(out var value))
      return value;

    return (float)number.GetDouble();
  }

  private static string NormalizeEndpoint(string endpoint)
  {
    if (string.IsNullOrWhiteSpace(endpoint))
      return "/";

    return endpoint.StartsWith('/') ? endpoint : $"/{endpoint}";
  }

  private sealed record EmbeddingAttempt(string Endpoint, object Payload);
  private sealed record EmbeddingAttemptResponse(OperationResult<float[]> Result, bool IsNotFound);
}
