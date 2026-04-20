namespace CleanArc.Infrastructure.Persistence.Settings;

public class CloudinaryStickerOptions
{
  public const string SectionName = nameof(CloudinaryStickerOptions);

  public string CloudName { get; set; } = string.Empty;
  public string ApiKey { get; set; } = string.Empty;
  public string ApiSecret { get; set; } = string.Empty;
  public string Folder { get; set; } = "vega/stickers";
}
