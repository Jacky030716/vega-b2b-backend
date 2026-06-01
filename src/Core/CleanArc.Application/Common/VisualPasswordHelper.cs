using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CleanArc.Application.Common;

public static class VisualPasswordHelper
{
    private static readonly Regex VisualPasswordRegex = new("^icon_\\d{2}-icon_\\d{2}-icon_\\d{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly string[] VisualIconPool =
    [
        "icon_01",
        "icon_02",
        "icon_03",
        "icon_04",
        "icon_05",
        "icon_06",
        "icon_07",
        "icon_08",
        "icon_09",
        "icon_10",
        "icon_11",
        "icon_12"
    ];

    public static string HashPassword(string visualSequence, string loginCode)
    {
        if (string.IsNullOrWhiteSpace(visualSequence))
            throw new ArgumentException("Visual sequence cannot be empty", nameof(visualSequence));
        if (string.IsNullOrWhiteSpace(loginCode))
            throw new ArgumentException("Login code cannot be empty", nameof(loginCode));

        var normalizedSequence = visualSequence.Trim().ToLowerInvariant();
        var salt = loginCode.Trim().ToUpperInvariant();
        var saltedInput = $"{salt}:{normalizedSequence}";
        
        var bytes = Encoding.UTF8.GetBytes(saltedInput);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool VerifyPassword(string visualSequence, string loginCode, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(visualSequence) || string.IsNullOrWhiteSpace(loginCode) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        // Backward compatibility support for BCrypt hashes if they exist
        if (storedHash.StartsWith("$2") || storedHash.Length == 60)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(visualSequence, storedHash);
            }
            catch
            {
                return false;
            }
        }

        var computedHash = HashPassword(visualSequence, loginCode);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(storedHash)
        );
    }

    public static string GenerateVisualPassword()
    {
        var first = VisualIconPool[RandomNumberGenerator.GetInt32(0, VisualIconPool.Length)];
        var second = VisualIconPool[RandomNumberGenerator.GetInt32(0, VisualIconPool.Length)];
        var third = VisualIconPool[RandomNumberGenerator.GetInt32(0, VisualIconPool.Length)];
        return $"{first}-{second}-{third}";
    }

    public static bool IsValidVisualPassword(string providedVisualPassword)
    {
        if (string.IsNullOrWhiteSpace(providedVisualPassword))
            return false;

        if (!VisualPasswordRegex.IsMatch(providedVisualPassword))
            return false;

        var tokens = providedVisualPassword.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length == 3 && tokens.All(token => VisualIconPool.Contains(token, StringComparer.OrdinalIgnoreCase));
    }
}
