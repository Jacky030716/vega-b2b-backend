namespace CleanArc.Infrastructure.Persistence.Settings;

public class HuggingFaceStickerOptions
{
  public const string SectionName = nameof(HuggingFaceStickerOptions);

  public string ApiBaseUrl { get; set; } = "https://router.huggingface.co/hf-inference/models";
  public string ModelId { get; set; } = "black-forest-labs/FLUX.1-schnell";
  public string ApiToken { get; set; } = string.Empty;
  public int RequestTimeoutSeconds { get; set; } = 120;
  public int Width { get; set; } = 512;
  public int Height { get; set; } = 512;
  public string NegativePrompt { get; set; } = "text, watermark, blurry, low quality, cropped, truncated, cut off, out of frame";
}
