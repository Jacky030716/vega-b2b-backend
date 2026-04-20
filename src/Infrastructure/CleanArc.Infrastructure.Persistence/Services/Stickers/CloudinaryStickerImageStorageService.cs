using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.Stickers;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.Stickers;

public class CloudinaryStickerImageStorageService : IStickerImageStorageService
{
  private readonly HttpClient _httpClient;
  private readonly CloudinaryStickerOptions _options;

  public CloudinaryStickerImageStorageService(
    HttpClient httpClient,
    IOptions<CloudinaryStickerOptions> options)
  {
    _httpClient = httpClient;
    _options = options.Value;
  }

  public async Task<OperationResult<StickerUploadResult>> UploadPngAsync(byte[] imageBytes, string fileName, CancellationToken cancellationToken)
  {
    if (imageBytes is null || imageBytes.Length == 0)
      return OperationResult<StickerUploadResult>.FailureResult("Sticker upload received an empty image payload.");

    if (string.IsNullOrWhiteSpace(_options.CloudName)
      || string.IsNullOrWhiteSpace(_options.ApiKey)
      || string.IsNullOrWhiteSpace(_options.ApiSecret))
      return OperationResult<StickerUploadResult>.FailureResult("Sticker storage provider is not configured.");

    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var safeFileName = SanitizeFileName(fileName);
    var publicId = $"{_options.Folder.Trim('/')}/{safeFileName}-{Guid.NewGuid():N}";
    var signature = ComputeSignature(_options.Folder, publicId, timestamp, _options.ApiSecret);

    using var content = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(imageBytes);
    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

    content.Add(fileContent, "file", $"{safeFileName}.png");
    content.Add(new StringContent(_options.ApiKey), "api_key");
    content.Add(new StringContent(timestamp.ToString()), "timestamp");
    content.Add(new StringContent(_options.Folder), "folder");
    content.Add(new StringContent(publicId), "public_id");
    content.Add(new StringContent(signature), "signature");

    var endpoint = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/image/upload";
    using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
    var body = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
      return OperationResult<StickerUploadResult>.FailureResult($"Sticker upload failed ({(int)response.StatusCode}): {body}");

    using var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;

    if (!root.TryGetProperty("public_id", out var publicIdNode)
      || !root.TryGetProperty("secure_url", out var secureUrlNode))
      return OperationResult<StickerUploadResult>.FailureResult("Sticker upload response was missing required fields.");

    return OperationResult<StickerUploadResult>.SuccessResult(
      new StickerUploadResult(publicIdNode.GetString() ?? string.Empty, secureUrlNode.GetString() ?? string.Empty));
  }

  private static string ComputeSignature(string folder, string publicId, long timestamp, string secret)
  {
    var raw = $"folder={folder}&public_id={publicId}&timestamp={timestamp}{secret}";
    var bytes = Encoding.UTF8.GetBytes(raw);
    var hash = SHA1.HashData(bytes);
    return Convert.ToHexString(hash).ToLowerInvariant();
  }

  private static string SanitizeFileName(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
      return "sticker";

    var cleaned = new string(input
      .Trim()
      .ToLowerInvariant()
      .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
      .ToArray());

    while (cleaned.Contains("--", StringComparison.Ordinal))
      cleaned = cleaned.Replace("--", "-", StringComparison.Ordinal);

    return cleaned.Trim('-');
  }
}
