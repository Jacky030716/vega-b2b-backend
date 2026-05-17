using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.ClassroomThumbnails;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.Classrooms;

public sealed class GoogleClassroomThumbnailImageGenerationService(
  HttpClient httpClient,
  IOptions<GoogleImageAiOptions> options,
  IOptions<GoogleAiOptions> textOptions) : IClassroomThumbnailImageGenerationService
{
  private readonly HttpClient _httpClient = httpClient;
  private readonly GoogleImageAiOptions _options = options.Value;
  private readonly GoogleAiOptions _textOptions = textOptions.Value;

  public async Task<OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>> GenerateAsync(
    ClassroomThumbnailGenerationRequest request,
    CancellationToken cancellationToken)
  {
    var apiKey = ResolveApiKey(_options, _textOptions);
    if (string.IsNullOrWhiteSpace(apiKey))
      return OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>.FailureResult("Google image AI API key is not configured.");

    if (string.IsNullOrWhiteSpace(_options.ModelId))
      return OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>.FailureResult("Google image AI model id is not configured.");

    var endpoint = $"models/{Uri.EscapeDataString(_options.ModelId)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
    var prompt = BuildPrompt(request);
    var payload = new
    {
      contents = new[]
      {
        new
        {
          parts = new[] { new { text = prompt } }
        }
      },
      generationConfig = new
      {
        responseModalities = new[] { "TEXT", "IMAGE" },
        imageConfig = new
        {
          aspectRatio = "1:1"
        }
      }
    };

    try
    {
      using var aiTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 120));
      using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, aiTimeout.Token);
      var body = await response.Content.ReadAsStringAsync(aiTimeout.Token);

      if (!response.IsSuccessStatusCode)
        return OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>.FailureResult(BuildFailureMessage(response.StatusCode, body));

      var extraction = TryExtractImage(body);
      if (!extraction.IsSuccess)
        return OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>.FailureResult(extraction.ErrorMessage ?? "Google image AI response did not include an image.");

      return OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>.SuccessResult(
        (extraction.ImageBytes, extraction.MimeType, _options.ModelId));
    }
    catch (Exception ex)
    {
      return OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>.FailureResult($"Google image AI generation request failed: {ex.Message}");
    }
  }

  private static string BuildPrompt(ClassroomThumbnailGenerationRequest request)
  {
    var subjectText = request.Subjects.Count > 0 ? string.Join(", ", request.Subjects) : "classroom learning";
    var description = string.IsNullOrWhiteSpace(request.Description)
      ? string.Empty
      : $" Classroom context: {request.Description.Trim()}";

    return
      $"Create a child-safe square 1:1 classroom thumbnail for a Malaysian primary school learning app. Teacher request: {request.ThumbnailPrompt.Trim()}. Classroom: {request.ClassroomName.Trim()}, Year {request.YearLevel}, subjects: {subjectText}.{description} Use a playful Duolingo-inspired educational illustration style with books, learning icons, friendly colors, and no text overlay.";
  }

  private static string ResolveApiKey(GoogleImageAiOptions imageOptions, GoogleAiOptions textOptions)
  {
    var candidates = new[]
    {
      imageOptions.ApiKey,
      textOptions.ApiKey,
      Environment.GetEnvironmentVariable("GOOGLE_IMAGE_AI_API_KEY"),
      Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY"),
      Environment.GetEnvironmentVariable("GEMINI_API_KEY")
    };

    return candidates.FirstOrDefault(key => !IsPlaceholder(key)) ?? string.Empty;
  }

  private static bool IsPlaceholder(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return true;

    return false;
  }

  private static string BuildFailureMessage(HttpStatusCode statusCode, string body)
  {
    var upstreamError = TryExtractGoogleError(body);
    var detail = string.IsNullOrWhiteSpace(upstreamError) ? body : upstreamError;
    return $"Google image AI generation failed ({(int)statusCode}): {detail}";
  }

  private static string? TryExtractGoogleError(string body)
  {
    if (string.IsNullOrWhiteSpace(body))
      return null;

    try
    {
      using var doc = JsonDocument.Parse(body);
      if (!doc.RootElement.TryGetProperty("error", out var errorElement))
        return null;

      var message = errorElement.TryGetProperty("message", out var messageElement)
        ? messageElement.GetString()
        : null;

      var status = errorElement.TryGetProperty("status", out var statusElement)
        ? statusElement.GetString()
        : null;

      if (string.IsNullOrWhiteSpace(message))
        return errorElement.GetRawText();

      return string.IsNullOrWhiteSpace(status) ? message : $"{status}: {message}";
    }
    catch
    {
      return null;
    }
  }

  private static ImageExtractionResult TryExtractImage(string body)
  {
    try
    {
      using var doc = JsonDocument.Parse(body);
      var root = doc.RootElement;

      if (!root.TryGetProperty("candidates", out var candidates)
          || candidates.ValueKind != JsonValueKind.Array
          || candidates.GetArrayLength() == 0)
        return ImageExtractionResult.Failure("Google image AI response did not include candidates.");

      var firstCandidate = candidates[0];
      if (!firstCandidate.TryGetProperty("content", out var content)
          || !content.TryGetProperty("parts", out var parts)
          || parts.ValueKind != JsonValueKind.Array)
        return ImageExtractionResult.Failure("Google image AI response did not include content parts.");

      foreach (var part in parts.EnumerateArray())
      {
        if (!TryGetInlineData(part, out var inlineData))
          continue;

        var mimeType = inlineData.TryGetProperty("mimeType", out var mimeTypeElement)
          ? mimeTypeElement.GetString() ?? "image/png"
          : "image/png";

        if (!inlineData.TryGetProperty("data", out var dataElement))
          continue;

        var base64 = dataElement.GetString();
        if (string.IsNullOrWhiteSpace(base64))
          continue;

        return ImageExtractionResult.Success(Convert.FromBase64String(base64), mimeType);
      }

      return ImageExtractionResult.Failure("Google image AI response did not include inline image data.");
    }
    catch (Exception ex)
    {
      return ImageExtractionResult.Failure($"Google image AI response parsing failed: {ex.Message}");
    }
  }

  private static bool TryGetInlineData(JsonElement part, out JsonElement inlineData)
  {
    if (part.TryGetProperty("inlineData", out inlineData))
      return true;

    if (part.TryGetProperty("inline_data", out inlineData))
      return true;

    inlineData = default;
    return false;
  }

  private sealed record ImageExtractionResult(
    bool IsSuccess,
    byte[] ImageBytes,
    string MimeType,
    string? ErrorMessage)
  {
    public static ImageExtractionResult Success(byte[] imageBytes, string mimeType)
      => new(true, imageBytes, mimeType, null);

    public static ImageExtractionResult Failure(string errorMessage)
      => new(false, Array.Empty<byte>(), string.Empty, errorMessage);
  }
}
