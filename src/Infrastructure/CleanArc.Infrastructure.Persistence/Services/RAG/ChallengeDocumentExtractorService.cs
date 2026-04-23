using System.Text;
using System.Text.RegularExpressions;
using CleanArc.Application.Contracts.Infrastructure.Documents;
using CleanArc.Application.Models.Common;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace CleanArc.Infrastructure.Persistence.Services.RAG;

public class ChallengeDocumentExtractorService : IChallengeDocumentExtractor
{
  public Task<OperationResult<string>> ExtractTextAsync(
    ChallengeDocumentPayload document,
    CancellationToken cancellationToken)
  {
    if (document.Bytes.Length == 0)
      return Task.FromResult(OperationResult<string>.FailureResult("Uploaded syllabus file is empty."));

    var extension = Path.GetExtension(document.FileName)?.ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(extension))
      return Task.FromResult(OperationResult<string>.FailureResult("Unsupported document file extension."));

    try
    {
      var extracted = extension switch
      {
        ".txt" => ExtractTextFromTxt(document.Bytes),
        ".pdf" => ExtractTextFromPdf(document.Bytes),
        ".docx" => ExtractTextFromDocx(document.Bytes),
        ".doc" => ExtractTextFromDoc(document.Bytes),
        _ => null,
      };

      if (string.IsNullOrWhiteSpace(extracted))
      {
        return Task.FromResult(OperationResult<string>.FailureResult(
          "Unable to read text from the uploaded syllabus document."));
      }

      var normalized = NormalizeWhitespace(extracted);
      if (string.IsNullOrWhiteSpace(normalized))
      {
        return Task.FromResult(OperationResult<string>.FailureResult(
          "The syllabus document did not contain readable text."));
      }

      return Task.FromResult(OperationResult<string>.SuccessResult(normalized));
    }
    catch (Exception ex)
    {
      return Task.FromResult(OperationResult<string>.FailureResult($"Syllabus parsing failed: {ex.Message}"));
    }
  }

  private static string? ExtractTextFromTxt(byte[] bytes)
  {
    // UTF-8 first, then fallback to Unicode.
    var utf8 = Encoding.UTF8.GetString(bytes);
    if (!string.IsNullOrWhiteSpace(utf8))
      return utf8;

    return Encoding.Unicode.GetString(bytes);
  }

  private static string? ExtractTextFromPdf(byte[] bytes)
  {
    using var stream = new MemoryStream(bytes);
    using var document = PdfDocument.Open(stream);
    var pages = document.GetPages().Select(page => page.Text);
    return string.Join('\n', pages);
  }

  private static string? ExtractTextFromDocx(byte[] bytes)
  {
    using var stream = new MemoryStream(bytes);
    using var wordDocument = WordprocessingDocument.Open(stream, false);
    var textNodes = wordDocument.MainDocumentPart?
      .Document?
      .Descendants<Text>()
      .Select(node => node.Text)
      .Where(value => !string.IsNullOrWhiteSpace(value))
      .ToList();

    if (textNodes is null || textNodes.Count == 0)
      return null;

    return string.Join(' ', textNodes);
  }

  private static string? ExtractTextFromDoc(byte[] bytes)
  {
    // Best-effort fallback for legacy .doc files using binary text recovery.
    // A dedicated .doc parser package is preferred when available in the package feed.
    var unicode = Encoding.Unicode.GetString(bytes);
    var ascii = Encoding.ASCII.GetString(bytes);

    var unicodeClean = Regex.Replace(unicode, @"[^\u0009\u000A\u000D\u0020-\u024F]", " ");
    var asciiClean = Regex.Replace(ascii, @"[^\u0009\u000A\u000D\u0020-\u007E]", " ");

    var unicodeScore = ScoreText(unicodeClean);
    var asciiScore = ScoreText(asciiClean);
    return unicodeScore >= asciiScore ? unicodeClean : asciiClean;
  }

  private static string NormalizeWhitespace(string input)
  {
    return string.Join(' ', input
      .Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
      .Select(part => part.Trim()))
      .Trim();
  }

  private static int ScoreText(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return 0;

    return value.Count(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch));
  }
}
