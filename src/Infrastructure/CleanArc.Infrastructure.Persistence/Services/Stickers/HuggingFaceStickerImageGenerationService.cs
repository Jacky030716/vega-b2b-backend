using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.Stickers;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.Stickers;

public class HuggingFaceStickerImageGenerationService : IStickerImageGenerationService
{
  private readonly HttpClient _httpClient;
  private readonly HuggingFaceStickerOptions _options;

  public HuggingFaceStickerImageGenerationService(
    HttpClient httpClient,
    IOptions<HuggingFaceStickerOptions> options)
  {
    _httpClient = httpClient;
    _options = options.Value;
  }

  public async Task<OperationResult<StickerGenerationResult>> GenerateAsync(StickerGenerationRequest request, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(_options.ApiToken) || string.IsNullOrWhiteSpace(_options.ModelId))
      return OperationResult<StickerGenerationResult>.FailureResult("Sticker generation provider is not configured.");

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
      return OperationResult<StickerGenerationResult>.FailureResult(
        $"Sticker generation failed ({(int)response.StatusCode}): {errorBody}");
    }

    var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
    if (imageBytes.Length == 0)
      return OperationResult<StickerGenerationResult>.FailureResult("Sticker generation returned an empty image payload.");

    return OperationResult<StickerGenerationResult>.SuccessResult(new StickerGenerationResult(imageBytes, _options.ModelId));
  }

  private static string BuildPrompt(StickerGenerationRequest request)
  {
    var subject = request.Subject.Trim();
    var style = request.Style.Trim();
    var mood = request.Mood.Trim();

    return $"cute {subject} sticker, {style} style, {mood} expression, die-cut white outline, transparent background, centered, clean, high quality";
  }
}
