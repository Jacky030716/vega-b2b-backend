using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.Stickers;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.Stickers;

public class HuggingFaceStickerImageGenerationService : IStickerImageGenerationService
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<HuggingFaceStickerImageGenerationService> _logger;
  private readonly HuggingFaceStickerOptions _options;

  public HuggingFaceStickerImageGenerationService(
    HttpClient httpClient,
    ILogger<HuggingFaceStickerImageGenerationService> logger,
    IOptions<HuggingFaceStickerOptions> options)
  {
    _httpClient = httpClient;
    _logger = logger;
    _options = options.Value;
  }

  public async Task<OperationResult<StickerGenerationResult>> GenerateAsync(StickerGenerationRequest request, CancellationToken cancellationToken)
  {
    var apiToken = _options.ApiToken;

    if (string.IsNullOrWhiteSpace(apiToken) || string.IsNullOrWhiteSpace(_options.ModelId))
      return OperationResult<StickerGenerationResult>.FailureResult("Hugging Face sticker generation is not configured yet.");

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
        "Hugging Face sticker generation failed with status {StatusCode} for model {ModelId}. Body: {ErrorBody}",
        (int)response.StatusCode,
        _options.ModelId,
        errorBody);

      return OperationResult<StickerGenerationResult>.FailureResult(
        $"Hugging Face sticker generation failed ({(int)response.StatusCode}): {errorBody}");
    }

    var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
    if (imageBytes.Length == 0)
      return OperationResult<StickerGenerationResult>.FailureResult("Hugging Face returned an empty sticker image.");

    return OperationResult<StickerGenerationResult>.SuccessResult(new StickerGenerationResult(imageBytes, _options.ModelId));
  }

  private static string BuildPrompt(StickerGenerationRequest request)
  {
    var subject = request.Subject.Trim();
    var style = request.Style.Trim();
    var mood = request.Mood.Trim();

    return $"cute {subject} sticker, {style} style, {mood} expression, 1:1 square composition, transparent background, die-cut white outline, centered, fully visible subject, no cropping, no truncation, no cut off edges, clean, high quality, no text, no watermark";
  }

}
