using CleanArc.Application.Contracts.Audit;
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
    IAiUsageService aiUsageService,
    IAiRateLimitService aiRateLimitService,
    IUnitOfWork unitOfWork,
    IInstitutionUserReportRepository institutionUserReportRepository,
    IAuditRouter auditRouter,
    IAuditRouteHandler auditRouteHandler,
    IAuditFindingsSummarizer auditFindingsSummarizer)
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

        if (!string.Equals(institution.SubscriptionTier, "Premium", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<AskAuditorResult>.FailureResult("AI Diagnostic Auditing is only available to institutions on the Premium plan. Please upgrade your subscription to unlock this feature.");
        }

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
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            user.UserName,
            user.Role,
            user.IsActive,
            user.ClassName,
            user.HasLoggedIn,
            user.LastLoginAt
        }).ToList();

        var userNamesMap = users.ToDictionary(
            user => user.Id,
            user => $"{user.FirstName} {user.LastName}".Trim() is var name && !string.IsNullOrWhiteSpace(name) ? name : user.UserName);

        var route = auditRouter.Route(request.Question);
        if (!string.Equals(route.Intent, AuditIntentTypes.Unknown, StringComparison.Ordinal))
        {
            var routed = await auditRouteHandler.TryHandleAsync(
                route,
                new AuditRouteRequest(
                    request.InstitutionId,
                    request.UserId,
                    request.Question,
                    users.Select(user => new AuditRouteUserContext(
                        user.Id,
                        user.UserName,
                        user.Role,
                        user.ClassName,
                        userNamesMap[user.Id])).ToList()),
                cancellationToken);

            if (routed is not null)
            {
                var hybridResult = await BuildHybridAuditResponseAsync(
                    request,
                    institution.Name,
                    routed,
                    userNamesMap,
                    cancellationToken);

                return OperationResult<AskAuditorResult>.SuccessResult(hybridResult);
            }
        }

        var dataContext = new
        {
            InstitutionName = institution.Name,
            SeatCapacity = institution.MaxSeats,
            SeatsUsed = institution.UserMemberships.Count(membership => membership.IsActive),
            Subscription = institution.SubscriptionTier,
            RenewalDate = institution.RenewalDate.ToString("yyyy-MM-dd"),
            RawQuestion = request.Question,
            Users = userMetadata
        };

        var serializedContext = JsonSerializer.Serialize(dataContext);

        var prompt = promptRegistry.Get(AiUseCases.AdminAuditor);
        var systemPrompt = prompt.SystemInstruction + serializedContext;

        var aiRequest = new ChallengeGenerationRequest(
            SystemPrompt: systemPrompt,
            UserPrompt: request.Question,
            Temperature: 0.3,
            JsonMode: true
        );

        if (request.UserId is int userId)
        {
            var rateLimit = await aiRateLimitService.TryAcquireAsync(userId, AiFeatureTypes.AdminAuditor, cancellationToken);
            if (!rateLimit.Allowed)
            {
                return OperationResult<AskAuditorResult>.FailureResult("Too many AI requests. Please try again later.");
            }

            var quota = await aiUsageService.GetRemainingQuotaAsync(userId, AiFeatureTypes.AdminAuditor, cancellationToken);
            if (quota.Remaining <= 0)
            {
                return OperationResult<AskAuditorResult>.FailureResult("Your AI quota is exhausted for this month.");
            }
        }

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

        if (request.UserId is int consumeUserId)
        {
            await aiUsageService.ConsumeUsageAsync(
                consumeUserId,
                AiFeatureTypes.AdminAuditor,
                "POST /api/v1.1/advisor/auditor",
                "GEMINI",
                null,
                1,
                true,
                null,
                "institution",
                request.InstitutionId,
                cancellationToken);
        }

        return OperationResult<AskAuditorResult>.SuccessResult(new AskAuditorResult
        {
            Answer = parsed.Answer,
            MatchedUserIds = parsed.MatchedUserIds,
            MatchedUserNames = parsed.MatchedUserIds
                .Select(id => userNamesMap.TryGetValue(id, out var name) ? name : $"Student #{id}")
                .ToList()
        });
    }

    private async Task<AskAuditorResult> BuildHybridAuditResponseAsync(
        AskAuditorQuery request,
        string institutionName,
        AuditRouteResponse routed,
        Dictionary<int, string> userNamesMap,
        CancellationToken cancellationToken)
    {
        var findingsJson = AuditRouterResponseComposer.ExtractFindingsJson(routed.AnswerJson);
        string? summary = null;
        int? auditLogId = null;

        if (!string.IsNullOrWhiteSpace(findingsJson))
        {
            var governanceError = await ValidateAuditorGovernanceAsync(request.UserId, cancellationToken);
            if (governanceError is null)
            {
                var summaryPrompt = promptRegistry.Get(AiUseCases.AdminAuditorFindingsSummary);
                auditLogId = await aiAuditService.StartAsync(
                    new AiAuditStartRequest(
                        AiUseCases.AdminAuditorFindingsSummary,
                        "GEMINI",
                        null,
                        summaryPrompt.Version,
                        JsonSerializer.Serialize(new
                        {
                            request.InstitutionId,
                            InstitutionName = institutionName,
                            request.Question,
                            Findings = findingsJson
                        }),
                        RelatedUserId: request.UserId),
                    cancellationToken);

                summary = await auditFindingsSummarizer.SummarizeAsync(
                    request.Question,
                    findingsJson,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(summary))
                {
                    await aiAuditService.FailAsync(
                        auditLogId.Value,
                        null,
                        new[] { "Audit findings summary generation failed." },
                        cancellationToken);
                }
                else
                {
                    await aiAuditService.CompleteAsync(
                        auditLogId.Value,
                        summary,
                        JsonSerializer.Serialize(new { summary }),
                        AiValidationStatuses.Valid,
                        Array.Empty<string>(),
                        cancellationToken);

                    if (request.UserId is int consumeUserId)
                    {
                        await aiUsageService.ConsumeUsageAsync(
                            consumeUserId,
                            AiFeatureTypes.AdminAuditor,
                            "POST /api/v1.1/advisor/auditor/hybrid-summary",
                            "GEMINI",
                            null,
                            1,
                            true,
                            null,
                            "institution",
                            request.InstitutionId,
                            cancellationToken);
                    }
                }
            }
        }

        var answer = AuditRouterResponseComposer.AttachSummary(routed.AnswerJson, summary);
        return new AskAuditorResult
        {
            Answer = answer,
            MatchedUserIds = routed.MatchedUserIds,
            MatchedUserNames = routed.MatchedUserIds
                .Select(id => userNamesMap.TryGetValue(id, out var name) ? name : $"Student #{id}")
                .ToList()
        };
    }

    private async Task<string?> ValidateAuditorGovernanceAsync(int? userId, CancellationToken cancellationToken)
    {
        if (userId is not int resolvedUserId)
            return null;

        var rateLimit = await aiRateLimitService.TryAcquireAsync(resolvedUserId, AiFeatureTypes.AdminAuditor, cancellationToken);
        if (!rateLimit.Allowed)
            return "Too many AI requests. Please try again later.";

        var quota = await aiUsageService.GetRemainingQuotaAsync(resolvedUserId, AiFeatureTypes.AdminAuditor, cancellationToken);
        if (quota.Remaining <= 0)
            return "Your AI quota is exhausted for this month.";

        return null;
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
