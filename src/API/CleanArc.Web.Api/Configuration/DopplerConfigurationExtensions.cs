#nullable enable
using Microsoft.Extensions.Configuration;

namespace CleanArc.Web.Api.Configuration;

internal static class DopplerConfigurationExtensions
{
  public static IConfigurationManager AddDopplerSecretMappings(this IConfigurationManager configuration)
  {
    var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    AddIfPresent(overrides, "FIREBASE_WEB_API_KEY", "FirebaseStorageOptions:WebApiKey");
    AddIfPresent(overrides, "FIREBASE_STORAGE_BUCKET", "FirebaseStorageOptions:BucketName");
    AddIfPresent(overrides, "FIREBASE_SERVICE_ACCOUNT_JSON", "FirebaseStorageOptions:ServiceAccountJson");

    AddIfPresent(overrides, "POSTGRES_CONNECTION_STRING", "ConnectionStrings:PostgreSQL");
    AddIfPresent(overrides, "IDENTITY_SECRET_KEY", "IdentitySettings:SecretKey");
    AddIfPresent(overrides, "IDENTITY_ENCRYPT_KEY", "IdentitySettings:Encryptkey");

    AddIfPresent(overrides, "GOOGLE_AI_API_KEY", "GoogleAiOptions:ApiKey");
    AddIfPresent(overrides, "GEMINI_API_KEY", "GoogleAiOptions:ApiKey");
    AddIfPresent(overrides, "GEMINI_API_KEY", "GoogleImageAiOptions:ApiKey");

    AddIfPresent(overrides, "HUGGING_FACE_API_KEY", "HuggingFaceStickerOptions:ApiToken");
    AddIfPresent(overrides, "HUGGINGFACE_STICKER_API_TOKEN", "HuggingFaceStickerOptions:ApiToken");

    AddIfPresent(overrides, "STRIPE_SECRET_KEY", "STRIPE_SECRET_KEY");
    AddIfPresent(overrides, "STRIPE_WEBHOOK_SECRET", "STRIPE_WEBHOOK_SECRET");

    if (overrides.Count > 0)
      configuration.AddInMemoryCollection(overrides);

    return configuration;
  }

  private static void AddIfPresent(IDictionary<string, string?> target, string sourceKey, string targetKey)
  {
    var value = Environment.GetEnvironmentVariable(sourceKey);
    if (!string.IsNullOrWhiteSpace(value))
      target[targetKey] = value;
  }
}
