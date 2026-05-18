namespace CleanArc.Infrastructure.Persistence.Settings;

public class FirebaseStorageOptions
{
  public const string SectionName = nameof(FirebaseStorageOptions);

  public string BucketName { get; set; } = string.Empty;
  public string WebApiKey { get; set; } = string.Empty;
  public string StickerFolder { get; set; } = "stickers/generated";
  public string ServiceAccountJson { get; set; } = string.Empty;
}
