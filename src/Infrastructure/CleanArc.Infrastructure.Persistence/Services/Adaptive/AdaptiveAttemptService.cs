using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;
public class AdaptiveAttemptService(
    ApplicationDbContext dbContext,
    IMasteryEngine masteryEngine) : IAdaptiveAttemptService
{
    public async Task<StartAdaptiveAttemptDto> StartAsync(
        StartAdaptiveAttemptRequest request,
        int authenticatedStudentId,
        CancellationToken cancellationToken)
    {
        var studentId = request.StudentId ?? authenticatedStudentId;
        var challengeExists = await dbContext.Challenges.AsNoTracking()
            .AnyAsync(c => c.Id == request.ChallengeId, cancellationToken);
        if (!challengeExists)
            throw new InvalidOperationException("Challenge not found");

        var attemptNo = (await dbContext.StudentChallengeAttempts.AsNoTracking()
            .Where(a => a.ChallengeId == request.ChallengeId && a.StudentId == studentId)
            .Select(a => (int?)a.AttemptNo)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        var attempt = new StudentChallengeAttempt
        {
            ChallengeId = request.ChallengeId,
            StudentId = studentId,
            AttemptNo = attemptNo,
            StartedAt = DateTime.UtcNow,
            CompletionStatus = "started",
            DeviceInfo = request.DeviceInfo
        };

        dbContext.StudentChallengeAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new StartAdaptiveAttemptDto(attempt.Id, attempt.ChallengeId, attempt.AttemptNo);
    }

    public async Task<StudentWordMasteryDto?> RecordItemAsync(SubmitAdaptiveItemAttemptRequest request, CancellationToken cancellationToken)
    {
        var itemAttempt = new StudentChallengeItemAttempt
        {
            StudentChallengeAttemptId = request.StudentChallengeAttemptId,
            ChallengeItemId = request.ChallengeItemId,
            VocabularyItemId = request.VocabularyItemId,
            GameTemplateId = request.GameTemplateId,
            PresentedAt = request.PresentedAt ?? DateTime.UtcNow,
            AnsweredAt = request.AnsweredAt ?? DateTime.UtcNow,
            ResponseTimeMs = request.ResponseTimeMs,
            WasCorrect = request.WasCorrect,
            FirstAttemptCorrect = request.FirstAttemptCorrect,
            RetriesCount = Math.Max(0, request.RetriesCount),
            HintsUsed = Math.Max(0, request.HintsUsed),
            AnswerText = request.AnswerText,
            ExpectedAnswerText = request.ExpectedAnswerText,
            SpeechConfidence = request.SpeechConfidence,
            ErrorType = request.ErrorType,
            RawTelemetryJson = string.IsNullOrWhiteSpace(request.RawTelemetryJson) ? "{}" : request.RawTelemetryJson
        };

        dbContext.StudentChallengeItemAttempts.Add(itemAttempt);
        await dbContext.SaveChangesAsync(cancellationToken);

        var mastery = await masteryEngine.ApplyItemAttemptAsync(request, cancellationToken);
        await LogErrorPatternAsync(request, itemAttempt.Id, cancellationToken);
        return mastery;
    }

    public async Task CompleteAsync(CompleteAdaptiveAttemptRequest request, CancellationToken cancellationToken)
    {
        var attempt = await dbContext.StudentChallengeAttempts
            .Include(a => a.ItemAttempts)
            .FirstOrDefaultAsync(a => a.Id == request.StudentChallengeAttemptId, cancellationToken)
            ?? throw new InvalidOperationException("Adaptive attempt not found");

        attempt.TotalScore = request.TotalScore;
        attempt.CompletionStatus = string.IsNullOrWhiteSpace(request.CompletionStatus)
            ? "completed"
            : request.CompletionStatus.Trim();
        attempt.CompletedAt = DateTime.UtcNow;
        attempt.TotalHintsUsed = attempt.ItemAttempts.Sum(i => i.HintsUsed);
        attempt.TotalRetries = attempt.ItemAttempts.Sum(i => i.RetriesCount);
        var responseTimes = attempt.ItemAttempts
            .Where(i => i.ResponseTimeMs.HasValue)
            .Select(i => i.ResponseTimeMs!.Value)
            .ToList();

        attempt.AverageResponseTimeMs = responseTimes.Count > 0
            ? (int)Math.Round(responseTimes.Average())
            : null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task LogErrorPatternAsync(
        SubmitAdaptiveItemAttemptRequest request,
        int itemAttemptId,
        CancellationToken cancellationToken)
    {
        if (request.WasCorrect && string.IsNullOrWhiteSpace(request.ErrorType))
            return;

        var attempt = await dbContext.StudentChallengeAttempts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.StudentChallengeAttemptId, cancellationToken);
        if (attempt is null) return;

        dbContext.ErrorPatternLogs.Add(new ErrorPatternLog
        {
            StudentId = attempt.StudentId,
            VocabularyItemId = request.VocabularyItemId,
            ChallengeItemAttemptId = itemAttemptId,
            PatternType = string.IsNullOrWhiteSpace(request.ErrorType) ? "incorrect_answer" : request.ErrorType.Trim(),
            ObservedValue = request.AnswerText,
            ExpectedValue = request.ExpectedAnswerText,
            MetadataJson = request.RawTelemetryJson ?? "{}",
            CreatedTime = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

