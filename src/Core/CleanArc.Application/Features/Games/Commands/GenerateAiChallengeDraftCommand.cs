using CleanArc.Application.Contracts.Infrastructure.Documents;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Commands;

/// <summary>
/// Editable AI draft response used by the frontend before final challenge save.
/// </summary>
public record GenerateAiChallengeDraftDto(
  string Title,
  string Description,
  string DraftSchema,
  string DraftPayload,
  string PlayableContentData,
  int? AiAuditLogId,
  string? SourceDocumentName,
  IReadOnlyList<RagChunkDto> RetrievedChunks);

/// <summary>
/// Retrieved context chunk metadata for debugging and transparency.
/// </summary>
public record RagChunkDto(string SourceLabel, string Content, double Similarity);

/// <summary>
/// Generates an editable AI draft for a specific game and classroom using prompt/doc sources.
/// </summary>
public record GenerateAiChallengeDraftCommand(
  int UserId,
  string GameKey,
  int ClassroomId,
  string? Prompt,
  ChallengeDocumentPayload? SourceDocument)
  : IRequest<OperationResult<GenerateAiChallengeDraftDto>>;
