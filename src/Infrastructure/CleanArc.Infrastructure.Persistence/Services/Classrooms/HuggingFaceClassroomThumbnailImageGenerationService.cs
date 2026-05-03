using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.ClassroomThumbnails;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.Classrooms;

public sealed class HuggingFaceClassroomThumbnailImageGenerationService(
  HttpClient httpClient,
  IOptions<HuggingFaceStickerOptions> options) : IClassroomThumbnailImageGenerationService
{
  private readonly HttpClient _httpClient = httpClient;
  private readonly HuggingFaceStickerOptions _options = options.Value;

  public async Task<OperationResult<(byte[] ImageBytes, string ModelName)>> GenerateAsync(
    ClassroomThumbnailGenerationRequest request,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(_options.ApiToken) || string.IsNullOrWhiteSpace(_options.ModelId))
      return OperationResult<(byte[] ImageBytes, string ModelName)>.FailureResult("Classroom thumbnail generation provider is not configured.");

    var prompt = BuildPrompt(request);
    var payload = new
    {
      inputs = prompt,
      parameters = new
      {
        width = _options.Width,
        height = _options.Height,
        negative_prompt = _options.NegativePrompt,
      },
      options = new
      {
        wait_for_model = true,
      },
    };

    var requestMessage = new HttpRequestMessage(HttpMethod.Post, _options.ModelId)
    {
      Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
    };
    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);

    using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
      return OperationResult<(byte[] ImageBytes, string ModelName)>.FailureResult(
        $"Classroom thumbnail generation failed ({(int)response.StatusCode}): {errorBody}");
    }

    var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
    if (imageBytes.Length == 0)
      return OperationResult<(byte[] ImageBytes, string ModelName)>.FailureResult("Classroom thumbnail generation returned an empty image payload.");

    return OperationResult<(byte[] ImageBytes, string ModelName)>.SuccessResult((imageBytes, _options.ModelId));
  }

  private static string BuildPrompt(ClassroomThumbnailGenerationRequest request)
  {
    var subjectText = request.Subjects.Count > 0 ? string.Join(", ", request.Subjects) : "classroom learning";
    var style = string.IsNullOrWhiteSpace(request.StylePreset) ? "playful learning" : request.StylePreset.Trim();
    var description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : $" The classroom description is: {request.Description.Trim()}";

    return
      $"Create a cute, colorful classroom thumbnail for a Year {request.YearLevel} {request.ClassroomName} class in a Malaysian primary school learning app. Include playful educational elements, books, icons, and a child-safe {style} aesthetic. Subject focus: {subjectText}.{description} No text overlay. Square composition suitable for a classroom card.";
  }
}
