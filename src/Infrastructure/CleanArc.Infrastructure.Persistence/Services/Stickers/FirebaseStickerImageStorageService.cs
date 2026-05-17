using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.Stickers;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.Stickers;

public sealed class FirebaseStickerImageStorageService : IStickerImageStorageService
{
  private const string IdentityToolkitSignUpEndpoint = "https://identitytoolkit.googleapis.com/v1/accounts:signUp";

  private readonly HttpClient _httpClient;
  private readonly ILogger<FirebaseStickerImageStorageService> _logger;
  private readonly FirebaseStorageOptions _options;
  private static readonly SemaphoreSlim TokenLock = new(1, 1);
  private static string s_idToken = string.Empty;
  private static DateTimeOffset s_idTokenExpiresAtUtc;

  public FirebaseStickerImageStorageService(
    HttpClient httpClient,
    IOptions<FirebaseStorageOptions> options,
    ILogger<FirebaseStickerImageStorageService> logger)
  {
    _httpClient = httpClient;
    _options = options.Value;
    _logger = logger;
  }

  public async Task<OperationResult<StickerUploadResult>> UploadAsync(
    byte[] imageBytes,
    string fileName,
    CancellationToken cancellationToken)
  {
    if (imageBytes is null || imageBytes.Length == 0)
      return OperationResult<StickerUploadResult>.FailureResult("Sticker upload received an empty image payload.");

    var bucketName = ResolveBucketName();
    var webApiKey = ResolveWebApiKey();
    if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(webApiKey))
      return OperationResult<StickerUploadResult>.FailureResult("Firebase sticker storage is not configured.");

    try
    {
      var imageFormat = ResolveImageFormat(imageBytes);
      var imageRef = CreateImageRef(fileName, imageFormat.Extension);
      var idToken = await GetFirebaseIdTokenAsync(webApiKey, cancellationToken);

      var uploadEndpoint =
        $"https://firebasestorage.googleapis.com/v0/b/{Uri.EscapeDataString(bucketName)}/o?name={Uri.EscapeDataString(imageRef)}";

      using var request = new HttpRequestMessage(HttpMethod.Post, uploadEndpoint);
      request.Headers.TryAddWithoutValidation("Authorization", $"Firebase {idToken}");
      request.Content = new ByteArrayContent(imageBytes);
      request.Content.Headers.ContentType = new MediaTypeHeaderValue(imageFormat.ContentType);

      using var response = await _httpClient.SendAsync(request, cancellationToken);
      var body = await response.Content.ReadAsStringAsync(cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        _logger.LogWarning(
          "Firebase sticker upload failed with status {StatusCode} for path {ImageRef}: {ResponseBody}",
          (int)response.StatusCode,
          imageRef,
          body);
        return OperationResult<StickerUploadResult>.FailureResult("Sticker upload failed. Please try again.");
      }

      return OperationResult<StickerUploadResult>.SuccessResult(new StickerUploadResult(imageRef));
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Firebase sticker upload failed before the sticker could be stored.");
      return OperationResult<StickerUploadResult>.FailureResult("Sticker upload failed. Please try again.");
    }
  }

  private async Task<string> GetFirebaseIdTokenAsync(string webApiKey, CancellationToken cancellationToken)
  {
    if (!string.IsNullOrWhiteSpace(s_idToken) && s_idTokenExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(5))
      return s_idToken;

    await TokenLock.WaitAsync(cancellationToken);
    try
    {
      if (!string.IsNullOrWhiteSpace(s_idToken) && s_idTokenExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(5))
        return s_idToken;

      var endpoint = $"{IdentityToolkitSignUpEndpoint}?key={Uri.EscapeDataString(webApiKey)}";
      using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
      {
        Content = new StringContent("{\"returnSecureToken\":true}", Encoding.UTF8, "application/json")
      };

      using var response = await _httpClient.SendAsync(request, cancellationToken);
      var body = await response.Content.ReadAsStringAsync(cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        _logger.LogWarning(
          "Firebase anonymous auth failed with status {StatusCode}: {ResponseBody}",
          (int)response.StatusCode,
          body);
        throw new InvalidOperationException("Firebase authentication failed.");
      }

      using var doc = JsonDocument.Parse(body);
      var root = doc.RootElement;
      if (!root.TryGetProperty("idToken", out var idTokenNode))
        throw new InvalidOperationException("Firebase authentication response was missing an id token.");

      s_idToken = idTokenNode.GetString();
      var expiresInSeconds = 3600;
      if (root.TryGetProperty("expiresIn", out var expiresInNode)
        && int.TryParse(expiresInNode.GetString(), out var parsedExpiresIn))
      {
        expiresInSeconds = parsedExpiresIn;
      }

      s_idTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);
      if (string.IsNullOrWhiteSpace(s_idToken))
        throw new InvalidOperationException("Firebase authentication response was empty.");

      return s_idToken;
    }
    finally
    {
      TokenLock.Release();
    }
  }

  private string ResolveBucketName() =>
    FirstConfiguredValue(_options.BucketName, Environment.GetEnvironmentVariable("FIREBASE_STORAGE_BUCKET"));

  private string ResolveWebApiKey() =>
    FirstConfiguredValue(_options.WebApiKey, Environment.GetEnvironmentVariable("FIREBASE_WEB_API_KEY"));

  private string CreateImageRef(string fileName, string extension)
  {
    var folder = string.IsNullOrWhiteSpace(_options.StickerFolder)
      ? "stickers/generated"
      : _options.StickerFolder.Trim('/');
    var safeFileName = SanitizeFileName(fileName);

    return $"{folder}/{safeFileName}-{Guid.NewGuid():N}.{extension}";
  }

  private static (string ContentType, string Extension) ResolveImageFormat(byte[] imageBytes)
  {
    if (imageBytes.Length >= 3
      && imageBytes[0] == 0xFF
      && imageBytes[1] == 0xD8
      && imageBytes[2] == 0xFF)
      return ("image/jpeg", "jpg");

    if (imageBytes.Length >= 12
      && imageBytes[0] == 'R'
      && imageBytes[1] == 'I'
      && imageBytes[2] == 'F'
      && imageBytes[3] == 'F'
      && imageBytes[8] == 'W'
      && imageBytes[9] == 'E'
      && imageBytes[10] == 'B'
      && imageBytes[11] == 'P')
      return ("image/webp", "webp");

    return ("image/png", "png");
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

  private static string FirstConfiguredValue(params string[] values) =>
    values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
}
