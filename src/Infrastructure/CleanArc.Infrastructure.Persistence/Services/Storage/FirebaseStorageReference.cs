#nullable enable

namespace CleanArc.Infrastructure.Persistence.Services.Storage;

internal static class FirebaseStorageReference
{
  public static string? Normalize(string? value)
  {
    var raw = value?.Trim();
    if (string.IsNullOrWhiteSpace(raw))
      return null;

    if (raw.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
    {
      var withoutScheme = raw["gs://".Length..];
      var slashIndex = withoutScheme.IndexOf('/', StringComparison.Ordinal);
      return slashIndex < 0 ? null : NormalizePath(withoutScheme[(slashIndex + 1)..]);
    }

    if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
      return NormalizePath(raw);

    if (string.Equals(uri.Host, "firebasestorage.googleapis.com", StringComparison.OrdinalIgnoreCase))
    {
      const string objectMarker = "/o/";
      var markerIndex = uri.AbsolutePath.IndexOf(objectMarker, StringComparison.Ordinal);
      if (markerIndex >= 0)
      {
        var encodedPath = uri.AbsolutePath[(markerIndex + objectMarker.Length)..];
        return NormalizePath(Uri.UnescapeDataString(encodedPath));
      }
    }

    if (string.Equals(uri.Host, "storage.googleapis.com", StringComparison.OrdinalIgnoreCase))
    {
      var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
      if (segments.Length >= 2)
        return NormalizePath(string.Join('/', segments.Skip(1)));
    }

    return raw;
  }

  public static string Require(string value, string fieldName)
  {
    return Normalize(value)
      ?? throw new InvalidOperationException($"{fieldName} must contain a Firebase Storage object path.");
  }

  private static string? NormalizePath(string value)
  {
    var normalized = value.Trim().TrimStart('/').Replace('\\', '/');
    return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
  }
}
