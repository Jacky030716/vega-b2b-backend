namespace CleanArc.Infrastructure.Persistence.Settings;

public class HuggingFaceStickerOptions
{
  public const string SectionName = nameof(HuggingFaceStickerOptions);

  public string ApiBaseUrl { get; set; } = "https://api-inference.huggingface.co/models";
  public string ModelId { get; set; } = string.Empty;
  public string ApiToken { get; set; } = string.Empty;
  public int RequestTimeoutSeconds { get; set; } = 60;
  public int Width { get; set; } = 512;
  public int Height { get; set; } = 512;
  public string NegativePrompt { get; set; } = "text, watermark, blurry, low quality";
}
