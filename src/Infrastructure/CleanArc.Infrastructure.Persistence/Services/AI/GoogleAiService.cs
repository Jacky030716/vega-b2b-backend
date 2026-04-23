using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.AI;

public sealed class GoogleAiService(
  HttpClient httpClient,
  IOptions<GoogleAiOptions> options)
  : IAiGenerationService
{
  private readonly HttpClient _httpClient = httpClient;
  private readonly GoogleAiOptions _options = options.Value;

  public async Task<OperationResult<ChallengeGenerationResult>> GenerateJsonAsync(
    ChallengeGenerationRequest request,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(_options.ApiKey)
        || _options.ApiKey.Equals("PASTE_YOUR_KEY_HERE", StringComparison.OrdinalIgnoreCase))
    {
      return OperationResult<ChallengeGenerationResult>.FailureResult(
        "Google AI API key is not configured.");
    }

    var modelId = string.IsNullOrWhiteSpace(request.Model)
      ? _options.ModelId
      : request.Model.Trim();

    if (string.IsNullOrWhiteSpace(modelId))
    {
      return OperationResult<ChallengeGenerationResult>.FailureResult(
        "Google AI model id is not configured.");
    }

    var endpoint =
      $"models/{Uri.EscapeDataString(modelId)}:generateContent?key={Uri.EscapeDataString(_options.ApiKey)}";

    var payload = new
    {
      systemInstruction = new
      {
        parts = new[] { new { text = request.SystemPrompt } },
      },
      contents = new[]
      {
        new
        {
          parts = new[] { new { text = request.UserPrompt } },
        },
      },
      generationConfig = new
      {
        response_mime_type = request.JsonMode ? "application/json" : "text/plain",
        temperature = request.Temperature,
      },
    };

    try
    {
      // Use an independent timeout — do NOT pass the HTTP request's cancellationToken.
      // When the mobile client disconnects (HTTP 499), ASP.NET cancels the request token,
      // which would abort the in-flight Google AI call. AI generation can take 30-60s.
      using var aiTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));

      using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, aiTimeout.Token);
      var body = await response.Content.ReadAsStringAsync(aiTimeout.Token);

      if (!response.IsSuccessStatusCode)
      {
        var errorMessage = BuildGenerationFailureMessage(response.StatusCode, body);
        return OperationResult<ChallengeGenerationResult>.FailureResult(errorMessage);
      }

      var extraction = TryExtractGeneratedText(body);
      if (!extraction.IsSuccess)
      {
        return OperationResult<ChallengeGenerationResult>.FailureResult(
          extraction.ErrorMessage ?? "Google AI response did not contain generated text.");
      }

      if (string.IsNullOrWhiteSpace(extraction.RawText))
      {
        return OperationResult<ChallengeGenerationResult>.FailureResult(
          "Google AI returned an empty generation payload.");
      }

      return OperationResult<ChallengeGenerationResult>.SuccessResult(
        new ChallengeGenerationResult(extraction.RawText));
    }
    catch (Exception ex)
    {
      return OperationResult<ChallengeGenerationResult>.FailureResult(
        $"Google AI generation request failed: {ex.Message}");
    }
  }

  private static string BuildGenerationFailureMessage(HttpStatusCode statusCode, string body)
  {
    var upstreamError = TryExtractGoogleError(body);
    var detail = string.IsNullOrWhiteSpace(upstreamError) ? body : upstreamError;
    return $"Google AI generation failed ({(int)statusCode}): {detail}";
  }

  private static string? TryExtractGoogleError(string body)
  {
    if (string.IsNullOrWhiteSpace(body))
    {
      return null;
    }

    try
    {
      using var doc = JsonDocument.Parse(body);
      if (!doc.RootElement.TryGetProperty("error", out var errorElement))
      {
        return null;
      }

      var message = errorElement.TryGetProperty("message", out var messageElement)
        ? messageElement.GetString()
        : null;

      var status = errorElement.TryGetProperty("status", out var statusElement)
        ? statusElement.GetString()
        : null;

      if (string.IsNullOrWhiteSpace(message))
      {
        return errorElement.GetRawText();
      }

      return string.IsNullOrWhiteSpace(status)
        ? message
        : $"{status}: {message}";
    }
    catch
    {
      return null;
    }
  }

  private static ExtractionResult TryExtractGeneratedText(string body)
  {
    try
    {
      using var doc = JsonDocument.Parse(body);
      var root = doc.RootElement;

      if (root.TryGetProperty("promptFeedback", out var promptFeedback)
          && promptFeedback.TryGetProperty("blockReason", out var blockReason)
          && blockReason.ValueKind == JsonValueKind.String)
      {
        var reason = blockReason.GetString();
        if (!string.IsNullOrWhiteSpace(reason))
        {
          return ExtractionResult.Failure($"Google AI blocked the response: {reason}");
        }
      }

      if (!root.TryGetProperty("candidates", out var candidates)
          || candidates.ValueKind != JsonValueKind.Array
          || candidates.GetArrayLength() == 0)
      {
        return ExtractionResult.Failure("Google AI response did not include candidates.");
      }

      var firstCandidate = candidates[0];
      if (!firstCandidate.TryGetProperty("content", out var content)
          || !content.TryGetProperty("parts", out var parts)
          || parts.ValueKind != JsonValueKind.Array)
      {
        return ExtractionResult.Failure("Google AI response did not include content parts.");
      }

      var textParts = parts
        .EnumerateArray()
        .Where(part => part.TryGetProperty("text", out _))
        .Select(part => part.GetProperty("text").GetString())
        .Where(text => !string.IsNullOrWhiteSpace(text))
        .ToList();

      if (textParts.Count == 0)
      {
        return ExtractionResult.Failure("Google AI candidate parts were empty.");
      }

      return ExtractionResult.Success(string.Join("\n", textParts));
    }
    catch (Exception ex)
    {
      return ExtractionResult.Failure($"Google AI response parsing failed: {ex.Message}");
    }
  }

  private sealed record ExtractionResult(bool IsSuccess, string RawText, string? ErrorMessage)
  {
    public static ExtractionResult Success(string rawText)
      => new(true, rawText, null);

    public static ExtractionResult Failure(string errorMessage)
      => new(false, string.Empty, errorMessage);
  }
}
