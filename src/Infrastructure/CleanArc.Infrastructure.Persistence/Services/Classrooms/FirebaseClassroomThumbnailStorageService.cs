using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.ClassroomThumbnails;
using CleanArc.Application.Models.Common;
using CleanArc.Infrastructure.Persistence.Settings;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArc.Infrastructure.Persistence.Services.Classrooms;

public sealed class FirebaseClassroomThumbnailStorageService : IClassroomThumbnailImageStorageService
{
  private const string IdentityToolkitSignUpEndpoint = "https://identitytoolkit.googleapis.com/v1/accounts:signUp";
  private const string DefaultFolder = "classrooms/thumbnails";

  private readonly HttpClient _httpClient;
  private readonly FirebaseStorageOptions _options;
  private readonly ILogger<FirebaseClassroomThumbnailStorageService> _logger;
  private static readonly SemaphoreSlim TokenLock = new(1, 1);
  private static string s_idToken = string.Empty;
  private static DateTimeOffset s_idTokenExpiresAtUtc;

  public FirebaseClassroomThumbnailStorageService(
    HttpClient httpClient,
    IOptions<FirebaseStorageOptions> options,
    ILogger<FirebaseClassroomThumbnailStorageService> logger)
  {
    _httpClient = httpClient;
    _options = options.Value;
    _logger = logger;
  }

  public async Task<OperationResult<ClassroomThumbnailUploadResult>> UploadAsync(
    byte[] imageBytes,
    string fileName,
    string contentType,
    CancellationToken cancellationToken)
  {
    if (imageBytes is null || imageBytes.Length == 0)
      return OperationResult<ClassroomThumbnailUploadResult>.FailureResult("Classroom thumbnail upload received an empty image payload.");

    var bucketName = _options.BucketName;
    var webApiKey = _options.WebApiKey;
    var saJson = _options.ServiceAccountJson;
    if (string.IsNullOrWhiteSpace(bucketName))
      return OperationResult<ClassroomThumbnailUploadResult>.FailureResult("Classroom thumbnail storage bucket is not configured.");

    try
    {
      var imageFormat = ResolveImageFormat(imageBytes, contentType);
      var imageRef = CreateImageRef(fileName, imageFormat.Extension);

      if (!string.IsNullOrWhiteSpace(saJson))
      {
        try
        {
          var credential = GoogleCredential.FromJson(saJson);
          var storage = StorageClient.Create(credential);

          using var ms = new MemoryStream(imageBytes);
          await storage.UploadObjectAsync(bucketName, imageRef, imageFormat.ContentType, ms, cancellationToken: cancellationToken);

          var publicUrl = BuildFirebasePublicUrl(bucketName, imageRef);
          return OperationResult<ClassroomThumbnailUploadResult>.SuccessResult(
            new ClassroomThumbnailUploadResult(imageRef, publicUrl));
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Service-account classroom thumbnail upload failed, falling back to anonymous upload.");
        }
      }

      if (string.IsNullOrWhiteSpace(webApiKey))
        return OperationResult<ClassroomThumbnailUploadResult>.FailureResult("Classroom thumbnail storage is not configured.");

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
          "Firebase classroom thumbnail upload failed with status {StatusCode} for path {ImageRef}: {ResponseBody}",
          (int)response.StatusCode,
          imageRef,
          body);
        return OperationResult<ClassroomThumbnailUploadResult>.FailureResult("Classroom thumbnail upload failed. Please try again.");
      }

      var url = BuildFirebasePublicUrl(bucketName, imageRef);
      return OperationResult<ClassroomThumbnailUploadResult>.SuccessResult(new ClassroomThumbnailUploadResult(imageRef, url));
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Firebase classroom thumbnail upload failed before the thumbnail could be stored.");
      return OperationResult<ClassroomThumbnailUploadResult>.FailureResult("Classroom thumbnail upload failed. Please try again.");
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

  private static string BuildFirebasePublicUrl(string bucketName, string imageRef)
  {
    var encodedPath = Uri.EscapeDataString(imageRef);
    return $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{encodedPath}?alt=media";
  }

  private static (string ContentType, string Extension) ResolveImageFormat(byte[] imageBytes, string contentType)
  {
    if (!string.IsNullOrWhiteSpace(contentType))
    {
      var normalized = contentType.ToLowerInvariant();
      if (normalized.Contains("jpeg") || normalized.Contains("jpg"))
        return (contentType, "jpg");
      if (normalized.Contains("webp"))
        return (contentType, "webp");
      if (normalized.Contains("png"))
        return (contentType, "png");

      return (contentType, "png");
    }

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

  private string CreateImageRef(string fileName, string extension)
  {
    var safeFileName = SanitizeFileName(fileName);
    return $"{DefaultFolder}/{safeFileName}-{Guid.NewGuid():N}.{extension}";
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
