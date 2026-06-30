using System.Text.Json;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Models.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Games.Commands;

internal sealed class GenerateSmartFeedbackCommandHandler(
    IAiGenerationService aiGenerationService,
    IAiPromptRegistry promptRegistry,
    IAiAuditService aiAuditService,
    IAiUsageService aiUsageService,
    IAiRateLimitService aiRateLimitService,
    ILogger<GenerateSmartFeedbackCommandHandler> logger)
    : IRequestHandler<GenerateSmartFeedbackCommand, OperationResult<SmartFeedbackResult>>
{
    public async ValueTask<OperationResult<SmartFeedbackResult>> Handle(GenerateSmartFeedbackCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received smart feedback request for game {GameName}, score {Score}, stars {StarsEarned}", 
            request.GameName, request.Score, request.StarsEarned);

        var dataContext = new
        {
            request.GameName,
            request.Score,
            request.StarsEarned,
            request.Accuracy
        };

        var serializedContext = JsonSerializer.Serialize(dataContext);

        var prompt = promptRegistry.Get(AiUseCases.SmartFeedbackGeneration);
        var systemPrompt = prompt.SystemInstruction + "\n\nContext:\n" + serializedContext;

        var aiRequest = new ChallengeGenerationRequest(
            SystemPrompt: systemPrompt,
            UserPrompt: "Please generate the feedback based on my recent game.",
            Temperature: 0.7,
            JsonMode: true
        );

        if (request.UserId is int userId)
        {
            var rateLimit = await aiRateLimitService.TryAcquireAsync(userId, AiFeatureTypes.SmartFeedbackGeneration, cancellationToken);
            if (!rateLimit.Allowed)
            {
                logger.LogWarning("Smart feedback rate limit hit for user {UserId}", userId);
                return OperationResult<SmartFeedbackResult>.FailureResult("Too many AI requests. Please try again later.");
            }

            var quota = await aiUsageService.GetRemainingQuotaAsync(userId, AiFeatureTypes.SmartFeedbackGeneration, cancellationToken);
            if (quota.Remaining <= 0)
            {
                logger.LogWarning("Smart feedback monthly quota exhausted for user {UserId}", userId);
                return OperationResult<SmartFeedbackResult>.FailureResult("Your AI quota is exhausted for this month.");
            }
        }

        var auditLogId = await aiAuditService.StartAsync(
            new AiAuditStartRequest(
                AiUseCases.SmartFeedbackGeneration,
                "GEMINI",
                null,
                prompt.Version,
                JsonSerializer.Serialize(dataContext),
                request.UserId),
            cancellationToken);

        logger.LogInformation("Starting smart feedback AI generation with audit log {AuditLogId}", auditLogId);

        var aiResult = await aiGenerationService.GenerateJsonAsync(aiRequest, cancellationToken);
        if (!aiResult.IsSuccess)
        {
            logger.LogError("AI feedback generation failed. Error: {Error}", aiResult.ErrorMessage);
            await aiAuditService.FailAsync(
                auditLogId,
                null,
                new[] { aiResult.ErrorMessage ?? "Failed to reach AI feedback generation." },
                cancellationToken);
            return OperationResult<SmartFeedbackResult>.FailureResult("Failed to reach AI feedback generation.");
        }

        var parsed = ParseFeedbackResponse(aiResult.Result.RawResponse);
        if (!parsed.IsValid)
        {
            logger.LogError("Failed to parse AI response. Errors: {Errors}. Raw Response: {Response}", 
                string.Join(", ", parsed.Errors), aiResult.Result.RawResponse);
            await aiAuditService.CompleteAsync(
                auditLogId,
                SanitizeJson(aiResult.Result.RawResponse),
                "{}",
                AiValidationStatuses.Invalid,
                parsed.Errors,
                cancellationToken);
            return OperationResult<SmartFeedbackResult>.FailureResult("Failed to parse AI feedback.");
        }

        await aiAuditService.CompleteAsync(
            auditLogId,
            SanitizeJson(aiResult.Result.RawResponse),
            JsonSerializer.Serialize(new { feedback = parsed.Feedback }),
            AiValidationStatuses.Valid,
            Array.Empty<string>(),
            cancellationToken);

        if (request.UserId is int consumeUserId)
        {
            await aiUsageService.ConsumeUsageAsync(
                consumeUserId,
                AiFeatureTypes.SmartFeedbackGeneration,
                "POST /api/v1.1/ai/game/smart-feedback",
                "GEMINI",
                null,
                1,
                true,
                null,
                null,
                null,
                cancellationToken);
        }

        logger.LogInformation("Smart feedback generated successfully.");
        return OperationResult<SmartFeedbackResult>.SuccessResult(new SmartFeedbackResult(parsed.Feedback));
    }

    private static FeedbackParseResult ParseFeedbackResponse(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return FeedbackParseResult.Invalid("AI returned an empty response.");
        }

        var normalized = SanitizeJson(rawResponse);

        try
        {
            using var doc = JsonDocument.Parse(normalized);
            var root = doc.RootElement;

            if (!root.TryGetProperty("feedback", out var feedbackElement) || feedbackElement.ValueKind != JsonValueKind.String)
                return FeedbackParseResult.Invalid("AI response is missing feedback.");

            var feedback = feedbackElement.GetString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(feedback))
                return FeedbackParseResult.Invalid("AI response feedback is empty.");

            return FeedbackParseResult.Valid(feedback);
        }
        catch
        {
            return FeedbackParseResult.Invalid("AI returned malformed JSON.");
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

    private sealed record FeedbackParseResult(
        bool IsValid,
        string Feedback,
        IReadOnlyList<string> Errors)
    {
        public static FeedbackParseResult Valid(string feedback)
            => new(true, feedback, Array.Empty<string>());

        public static FeedbackParseResult Invalid(string error)
            => new(false, string.Empty, new[] { error });
    }
}
