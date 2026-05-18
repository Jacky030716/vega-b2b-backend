using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.ClassroomThumbnails;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.Classrooms;

public sealed class HuggingFaceClassroomThumbnailImageGenerationService : IClassroomThumbnailImageGenerationService
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<HuggingFaceClassroomThumbnailImageGenerationService> _logger;
  private readonly HuggingFaceStickerOptions _options;

  public HuggingFaceClassroomThumbnailImageGenerationService(
    HttpClient httpClient,
    ILogger<HuggingFaceClassroomThumbnailImageGenerationService> logger,
    IOptions<HuggingFaceStickerOptions> options)
  {
    _httpClient = httpClient;
    _logger = logger;
    _options = options.Value;
  }

  public async Task<OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>> GenerateAsync(
    ClassroomThumbnailGenerationRequest request,
    CancellationToken cancellationToken)
  {
    var apiToken = _options.ApiToken;
    if (string.IsNullOrWhiteSpace(apiToken) || string.IsNullOrWhiteSpace(_options.ModelId))
      return OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>.FailureResult("Hugging Face classroom thumbnail generation is not configured yet.");

    var prompt = BuildPrompt(request);
    var parameters = new Dictionary<string, object>();
    if (_options.Width > 0)
      parameters["width"] = _options.Width;
    if (_options.Height > 0)
      parameters["height"] = _options.Height;
    if (!string.IsNullOrWhiteSpace(_options.NegativePrompt))
      parameters["negative_prompt"] = _options.NegativePrompt;

    var payload = new
    {
      inputs = prompt,
      parameters,
      options = new
      {
        wait_for_model = true,
      },
    };

    var requestMessage = new HttpRequestMessage(HttpMethod.Post, _options.ModelId)
    {
      Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
    };
    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

    using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
      _logger.LogWarning(
        "Hugging Face classroom thumbnail generation failed with status {StatusCode} for model {ModelId}. Body: {ErrorBody}",
        (int)response.StatusCode,
        _options.ModelId,
        errorBody);

      return OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>.FailureResult(
        $"Hugging Face classroom thumbnail generation failed ({(int)response.StatusCode}): {errorBody}");
    }

    var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
    if (imageBytes.Length == 0)
      return OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>.FailureResult("Hugging Face returned an empty classroom thumbnail image.");

    var mimeType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
    return OperationResult<(byte[] ImageBytes, string MimeType, string ModelName)>.SuccessResult((imageBytes, mimeType, _options.ModelId));
  }

  private static string BuildPrompt(ClassroomThumbnailGenerationRequest request)
  {
    var subjectText = request.Subjects.Count > 0 ? string.Join(", ", request.Subjects) : "classroom learning";
    var description = string.IsNullOrWhiteSpace(request.Description)
      ? string.Empty
      : $" Classroom context: {request.Description.Trim()}";

    return
      $"Create a child-safe classroom thumbnail for a Malaysian primary school learning app. Teacher request: {request.ThumbnailPrompt.Trim()}. Classroom: {request.ClassroomName.Trim()}, Year {request.YearLevel}, subjects: {subjectText}.{description} Use a playful educational illustration style with books, learning icons, friendly colors, a 1:1 square composition, a transparent background, centered and fully visible subjects, no cropping, no truncation, no cut off edges, and no text overlay.";
  }
}
