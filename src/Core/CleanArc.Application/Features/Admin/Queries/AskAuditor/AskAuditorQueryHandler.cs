using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;
using System.Text.Json;

namespace CleanArc.Application.Features.Admin.Queries.AskAuditor;

internal sealed class AskAuditorQueryHandler(
    IAiGenerationService aiGenerationService,
    IAiPromptRegistry promptRegistry,
    IAiAuditService aiAuditService,
    IUnitOfWork unitOfWork,
    IInstitutionUserReportRepository institutionUserReportRepository)
    : IRequestHandler<AskAuditorQuery, OperationResult<AskAuditorResult>>
{
    public async ValueTask<OperationResult<AskAuditorResult>> Handle(AskAuditorQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return OperationResult<AskAuditorResult>.FailureResult("Question is required.");
        }

        var institution = await unitOfWork.InstitutionRepository.GetInstitutionWithStatsAsync(request.InstitutionId);

        if (institution == null)
            return OperationResult<AskAuditorResult>.FailureResult("Institution not found.");

        var users = await institutionUserReportRepository.GetUsersAsync(
            new InstitutionUserReportFilter(
                InstitutionId: request.InstitutionId,
                Role: "all",
                Tab: "all",
                Search: null),
            cancellationToken);

        var userMetadata = users.Select(user => new
        {
            user.Id,
            user.UserName,
            user.Role,
            user.IsActive,
            user.ClassName,
            user.HasLoggedIn,
            user.LastLoginAt
        }).ToList();

        var dataContext = new
        {
            InstitutionName = institution.Name,
            SeatCapacity = institution.MaxSeats,
            SeatsUsed = institution.Users.Count,
            Subscription = institution.SubscriptionTier,
            RenewalDate = institution.RenewalDate.ToString("yyyy-MM-dd"),
            RawQuestion = request.Question,
            Users = userMetadata
        };

        var serializedContext = JsonSerializer.Serialize(dataContext);

        var prompt = promptRegistry.Get(AiUseCases.AdminAuditor);
        var systemPrompt = prompt.SystemInstruction + serializedContext;

        var aiRequest = new ChallengeGenerationRequest(
            Model: string.Empty,
            SystemPrompt: systemPrompt,
            UserPrompt: request.Question,
            Temperature: 0.3,
            JsonMode: true
        );

        var auditLogId = await aiAuditService.StartAsync(
            new AiAuditStartRequest(
                AiUseCases.AdminAuditor,
                "GEMINI",
                null,
                prompt.Version,
                JsonSerializer.Serialize(dataContext),
                request.UserId),
            cancellationToken);

        var aiResult = await aiGenerationService.GenerateJsonAsync(aiRequest, cancellationToken);
        if (!aiResult.IsSuccess)
        {
            await aiAuditService.FailAsync(
                auditLogId,
                null,
                new[] { aiResult.ErrorMessage ?? "Failed to reach Vega Auditor AI." },
                cancellationToken);
            return OperationResult<AskAuditorResult>.FailureResult("Failed to reach Vega Auditor AI.");
        }

        var parsed = ParseAuditorResponse(aiResult.Result.RawResponse, users.Select(x => x.Id).ToHashSet());
        if (!parsed.IsValid)
        {
            await aiAuditService.CompleteAsync(
                auditLogId,
                SanitizeJson(aiResult.Result.RawResponse),
                "{}",
                AiValidationStatuses.Invalid,
                parsed.Errors,
                cancellationToken);
            return OperationResult<AskAuditorResult>.FailureResult("Failed to reach Vega Auditor AI.");
        }

        await aiAuditService.CompleteAsync(
            auditLogId,
            SanitizeJson(aiResult.Result.RawResponse),
            JsonSerializer.Serialize(new { answer = parsed.Answer, matchedUserIds = parsed.MatchedUserIds }),
            AiValidationStatuses.Valid,
            Array.Empty<string>(),
            cancellationToken);

        return OperationResult<AskAuditorResult>.SuccessResult(new AskAuditorResult
        {
            Answer = parsed.Answer,
            MatchedUserIds = parsed.MatchedUserIds
        });
    }

    private static AuditorParseResult ParseAuditorResponse(
        string rawResponse,
        HashSet<int> validUserIds)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return AuditorParseResult.Invalid("AI auditor returned an empty response.");
        }

        var normalized = SanitizeJson(rawResponse);

        try
        {
            using var doc = JsonDocument.Parse(normalized);
            var root = doc.RootElement;

            if (!root.TryGetProperty("answer", out var answerElement) || answerElement.ValueKind != JsonValueKind.String)
                return AuditorParseResult.Invalid("AI auditor response is missing answer.");

            var answer = answerElement.GetString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(answer))
                return AuditorParseResult.Invalid("AI auditor response answer is empty.");

            var matchedIds = new List<int>();
            if (!root.TryGetProperty("matchedUserIds", out var idsElement) || idsElement.ValueKind != JsonValueKind.Array)
                return AuditorParseResult.Invalid("AI auditor response is missing matchedUserIds array.");

            foreach (var item in idsElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var id))
                    return AuditorParseResult.Invalid("AI auditor matchedUserIds must contain numbers only.");

                if (validUserIds.Contains(id))
                {
                    matchedIds.Add(id);
                }
            }

            return AuditorParseResult.Valid(answer, matchedIds.Distinct().ToList());
        }
        catch
        {
            return AuditorParseResult.Invalid("AI auditor returned malformed JSON.");
        }
    }

    private static string SanitizeJson(string rawResponse)
    {
        var normalized = rawResponse.Trim()
            .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("```", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
        return normalized;
    }

    private sealed record AuditorParseResult(
        bool IsValid,
        string Answer,
        IReadOnlyList<int> MatchedUserIds,
        IReadOnlyList<string> Errors)
    {
        public static AuditorParseResult Valid(string answer, IReadOnlyList<int> matchedUserIds)
            => new(true, answer, matchedUserIds, Array.Empty<string>());

        public static AuditorParseResult Invalid(string error)
            => new(false, string.Empty, Array.Empty<int>(), new[] { error });
    }
}
