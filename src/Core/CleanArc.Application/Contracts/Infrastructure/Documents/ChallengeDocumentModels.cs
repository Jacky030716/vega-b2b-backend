using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure.Documents;

/// <summary>
/// Raw uploaded syllabus document payload.
/// </summary>
public record ChallengeDocumentPayload(string FileName, string ContentType, byte[] Bytes);

/// <summary>
/// Service for extracting plain text from teacher-provided syllabus documents.
/// </summary>
public interface IChallengeDocumentExtractor
{
  /// <summary>
  /// Extracts normalized plain text from a supported document format.
  /// </summary>
  /// <param name="document">Document payload.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Extracted text wrapped in an operation result.</returns>
  Task<OperationResult<string>> ExtractTextAsync(
    ChallengeDocumentPayload document,
    CancellationToken cancellationToken);
}
