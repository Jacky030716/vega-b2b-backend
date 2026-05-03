using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.ClassroomThumbnails;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.Classrooms;

public sealed class CloudinaryClassroomThumbnailStorageService(
  HttpClient httpClient,
  IOptions<CloudinaryStickerOptions> options) : IClassroomThumbnailImageStorageService
{
  private readonly HttpClient _httpClient = httpClient;
  private readonly CloudinaryStickerOptions _options = options.Value;

  public async Task<OperationResult<ClassroomThumbnailUploadResult>> UploadAsync(
    byte[] imageBytes,
    string fileName,
    string contentType,
    CancellationToken cancellationToken)
  {
    if (imageBytes is null || imageBytes.Length == 0)
      return OperationResult<ClassroomThumbnailUploadResult>.FailureResult("Classroom thumbnail upload received an empty image payload.");

    if (string.IsNullOrWhiteSpace(_options.CloudName)
      || string.IsNullOrWhiteSpace(_options.ApiKey)
      || string.IsNullOrWhiteSpace(_options.ApiSecret))
      return OperationResult<ClassroomThumbnailUploadResult>.FailureResult("Classroom thumbnail storage provider is not configured.");

    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var safeFileName = SanitizeFileName(fileName);
    var folder = $"{_options.Folder.TrimEnd('/')}/classroom-thumbnails";
    var publicId = $"{folder}/{safeFileName}-{Guid.NewGuid():N}";
    var signature = ComputeSignature(folder, publicId, timestamp, _options.ApiSecret);

    using var content = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(imageBytes);
    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "image/png" : contentType);

    content.Add(fileContent, "file", $"{safeFileName}");
    content.Add(new StringContent(_options.ApiKey), "api_key");
    content.Add(new StringContent(timestamp.ToString()), "timestamp");
    content.Add(new StringContent(folder), "folder");
    content.Add(new StringContent(publicId), "public_id");
    content.Add(new StringContent(signature), "signature");

    var endpoint = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/image/upload";
    using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
    var body = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
      return OperationResult<ClassroomThumbnailUploadResult>.FailureResult($"Classroom thumbnail upload failed ({(int)response.StatusCode}): {body}");

    using var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;

    if (!root.TryGetProperty("public_id", out var publicIdNode)
      || !root.TryGetProperty("secure_url", out var secureUrlNode))
      return OperationResult<ClassroomThumbnailUploadResult>.FailureResult("Classroom thumbnail upload response was missing required fields.");

    return OperationResult<ClassroomThumbnailUploadResult>.SuccessResult(
      new ClassroomThumbnailUploadResult(publicIdNode.GetString() ?? string.Empty, secureUrlNode.GetString() ?? string.Empty));
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
      return "classroom-thumbnail";

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
