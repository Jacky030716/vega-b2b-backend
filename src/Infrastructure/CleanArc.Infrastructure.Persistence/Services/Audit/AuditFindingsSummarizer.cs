using System.Text.Json;
using CleanArc.Application.Contracts.Audit;
using CleanArc.Application.Contracts.Infrastructure.AI;

namespace CleanArc.Infrastructure.Persistence.Services.Audit;

public sealed class AuditFindingsSummarizer(
    IAiGenerationService aiGenerationService,
    IAiPromptRegistry promptRegistry) : IAuditFindingsSummarizer
{
    public async Task<string?> SummarizeAsync(
        string administratorQuestion,
        string findingsJson,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(findingsJson))
            return null;

        var prompt = promptRegistry.Get(AiUseCases.AdminAuditorFindingsSummary);
        var userPrompt = $"""
            Administrator question:
            {administratorQuestion.Trim()}

            Audit findings (authoritative JSON — do not add facts beyond this):
            {findingsJson}

            Summarize these findings for an administrator.
            """;

        var aiRequest = new ChallengeGenerationRequest(
            Model: "gemini-3.5-flash",
            SystemPrompt: prompt.SystemInstruction,
            UserPrompt: userPrompt,
            Temperature: 0.2,
            JsonMode: true);

        var aiResult = await aiGenerationService.GenerateJsonAsync(aiRequest, cancellationToken);
        if (!aiResult.IsSuccess || string.IsNullOrWhiteSpace(aiResult.Result.RawResponse))
            return null;

        return ParseSummary(aiResult.Result.RawResponse);
    }

    internal static string? ParseSummary(string rawResponse)
    {
        var normalized = rawResponse.Trim()
            .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("```", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        try
        {
            using var doc = JsonDocument.Parse(normalized);
            if (doc.RootElement.TryGetProperty("summary", out var summaryElement)
                && summaryElement.ValueKind == JsonValueKind.String)
            {
                var summary = summaryElement.GetString()?.Trim();
                return string.IsNullOrWhiteSpace(summary) ? null : summary;
            }
        }
        catch
        {
            // Fall through to plain-text fallback
        }

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
