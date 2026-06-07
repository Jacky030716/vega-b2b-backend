using CleanArc.Application.Contracts.Infrastructure.Documents;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Games.Commands;

public record GenerateAiChallengeDraftJobCommand(
    int AuditLogId,
    int UserId,
    string GameKey,
    int ClassroomId,
    string? Prompt,
    ChallengeDocumentPayload? SourceDocument,
    int? ModuleId = null,
    string? Mode = null)
    : IRequest<OperationResult<bool>>;
