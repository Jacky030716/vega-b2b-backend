using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using CleanArc.Application.Contracts.Infrastructure;

namespace CleanArc.Infrastructure.Persistence.Services;

public class StudentImportService : IStudentImportService
{
  private static readonly Regex LoginCodeRegex = new("^\\d{4}$", RegexOptions.Compiled);
  private static readonly Regex VisualPasswordRegex = new("^icon_\\d{2}-icon_\\d{2}-icon_\\d{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

  private static readonly string[] VisualIconPool =
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

  public IReadOnlyList<ParsedStudentCredential> ParseStudents(string csvContent)
  {
    if (string.IsNullOrWhiteSpace(csvContent))
      return [];

    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      TrimOptions = TrimOptions.Trim,
      HasHeaderRecord = false,
      IgnoreBlankLines = true,
      BadDataFound = null,
      MissingFieldFound = null
    };

    var results = new List<ParsedStudentCredential>();
    var generatedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    using var reader = new StringReader(csvContent);
    using var csvReader = new CsvReader(reader, config);

    bool isFirstRow = true;
    while (csvReader.Read())
    {
      var row = csvReader.Parser.Record;
      if (row is null || row.Length == 0)
        continue;

      var rawName = row[0]?.Trim();
      if (string.IsNullOrWhiteSpace(rawName))
        continue;

      var providedLoginCode = row.Length > 1 ? row[1]?.Trim() : null;
      var providedVisualPassword = row.Length > 2 ? row[2]?.Trim() : null;

      if (isFirstRow && IsHeaderRow(rawName, providedLoginCode, providedVisualPassword))
      {
        isFirstRow = false;
        continue;
      }

      isFirstRow = false;

      var normalizedName = NormalizeStudentName(rawName);
      if (string.IsNullOrWhiteSpace(normalizedName))
        continue;

      var loginCode = ResolveLoginCode(providedLoginCode, generatedCodes);
      var visualPassword = ResolveVisualPassword(providedVisualPassword);

      results.Add(new ParsedStudentCredential(normalizedName, loginCode, visualPassword));
    }

    return results;
  }

  private static string NormalizeStudentName(string name)
  {
    return string.Join(" ", name
        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
  }

  private static string GenerateUniqueFourDigitCode(HashSet<string> generatedCodes)
  {
    while (true)
    {
      var code = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
      if (generatedCodes.Add(code))
        return code;
    }
  }

  private static string GenerateVisualPassword()
  {
    var first = VisualIconPool[RandomNumberGenerator.GetInt32(0, VisualIconPool.Length)];
    var second = VisualIconPool[RandomNumberGenerator.GetInt32(0, VisualIconPool.Length)];
    var third = VisualIconPool[RandomNumberGenerator.GetInt32(0, VisualIconPool.Length)];
    return $"{first}-{second}-{third}";
  }

  private static bool IsHeaderRow(string name, string loginCode, string visualPassword)
  {
    var headerName = name.Equals("name", StringComparison.OrdinalIgnoreCase);
    if (!headerName)
      return false;

    var hasKnownSecondColumn = !string.IsNullOrWhiteSpace(loginCode)
      && loginCode.Contains("code", StringComparison.OrdinalIgnoreCase);

    var hasKnownThirdColumn = !string.IsNullOrWhiteSpace(visualPassword)
      && visualPassword.Contains("visual", StringComparison.OrdinalIgnoreCase);

    return hasKnownSecondColumn || hasKnownThirdColumn;
  }

  private static string ResolveLoginCode(string providedLoginCode, HashSet<string> generatedCodes)
  {
    if (!string.IsNullOrWhiteSpace(providedLoginCode)
      && LoginCodeRegex.IsMatch(providedLoginCode)
      && generatedCodes.Add(providedLoginCode))
    {
      return providedLoginCode;
    }

    return GenerateUniqueFourDigitCode(generatedCodes);
  }

  private static string ResolveVisualPassword(string providedVisualPassword)
  {
    if (string.IsNullOrWhiteSpace(providedVisualPassword))
      return GenerateVisualPassword();

    if (!VisualPasswordRegex.IsMatch(providedVisualPassword))
      return GenerateVisualPassword();

    var normalized = providedVisualPassword.ToLowerInvariant();
    var tokens = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
    var valid = tokens.Length == 3 && tokens.All(token => VisualIconPool.Contains(token, StringComparer.OrdinalIgnoreCase));
    return valid ? normalized : GenerateVisualPassword();
  }
}