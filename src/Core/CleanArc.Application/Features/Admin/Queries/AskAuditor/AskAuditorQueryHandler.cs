using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;
using System.Text.Json;

namespace CleanArc.Application.Features.Admin.Queries.AskAuditor;

internal sealed class AskAuditorQueryHandler(
    IAiGenerationService aiGenerationService,
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

        var systemPrompt = """
You are "Vega Auditor", an AI data assistant for institution administrators.
You must only use the JSON context provided and never invent values.
Return JSON only (no markdown, no code fences) with this shape:
{"answer":"string","matchedUserIds":[1,2,3]}
Rules:
- "answer": concise and actionable.
- "matchedUserIds": user IDs relevant to the question. If none, return [].
Context:
""" + serializedContext;

        var aiRequest = new ChallengeGenerationRequest(
            Model: "gemini-1.5-flash",
            SystemPrompt: systemPrompt,
            UserPrompt: request.Question,
            Temperature: 0.3,
            JsonMode: true
        );

        var aiResult = await aiGenerationService.GenerateJsonAsync(aiRequest, cancellationToken);
        if (!aiResult.IsSuccess)
        {
            return OperationResult<AskAuditorResult>.FailureResult("Failed to reach Vega Auditor AI.");
        }

        var parsed = ParseAuditorResponse(aiResult.Result.RawResponse, users.Select(x => x.Id).ToHashSet());

        return OperationResult<AskAuditorResult>.SuccessResult(new AskAuditorResult
        {
            Answer = parsed.Answer,
            MatchedUserIds = parsed.MatchedUserIds
        });
    }

    private static (string Answer, IReadOnlyList<int> MatchedUserIds) ParseAuditorResponse(
        string rawResponse,
        HashSet<int> validUserIds)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return (string.Empty, []);
        }

        var normalized = rawResponse.Trim()
            .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("```", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        try
        {
            using var doc = JsonDocument.Parse(normalized);
            var root = doc.RootElement;

            var answer = root.TryGetProperty("answer", out var answerElement)
                ? answerElement.GetString() ?? string.Empty
                : normalized;

            var matchedIds = new List<int>();
            if (root.TryGetProperty("matchedUserIds", out var idsElement) && idsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in idsElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Number &&
                        item.TryGetInt32(out var id) &&
                        validUserIds.Contains(id))
                    {
                        matchedIds.Add(id);
                    }
                }
            }

            return (answer, matchedIds.Distinct().ToList());
        }
        catch
        {
            return (normalized, []);
        }
    }
}
