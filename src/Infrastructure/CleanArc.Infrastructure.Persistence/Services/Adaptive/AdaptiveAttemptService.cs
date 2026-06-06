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

        var wasAlreadyCompleted = string.Equals(attempt.CompletionStatus, "completed", StringComparison.OrdinalIgnoreCase);

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

        if (!wasAlreadyCompleted && string.Equals(attempt.CompletionStatus, "completed", StringComparison.OrdinalIgnoreCase))
        {
            await UpdateWordProgressAfterCompletionAsync(attempt, cancellationToken);
        }
    }

    private async Task UpdateWordProgressAfterCompletionAsync(StudentChallengeAttempt attempt, CancellationToken cancellationToken)
    {
        var itemAttempts = attempt.ItemAttempts
            .Where(i => i.VocabularyItemId.HasValue)
            .ToList();

        if (itemAttempts.Count == 0) return;

        var vocabularyItemIds = itemAttempts.Select(i => i.VocabularyItemId!.Value).Distinct().ToList();

        var existingProgresses = await dbContext.WordProgresses
            .Where(wp => wp.StudentId == attempt.StudentId && vocabularyItemIds.Contains(wp.WordId))
            .ToListAsync(cancellationToken);

        var progressMap = existingProgresses.ToDictionary(wp => wp.WordId);

        foreach (var wordId in vocabularyItemIds)
        {
            var wordAttempts = itemAttempts.Where(i => i.VocabularyItemId == wordId).ToList();
            if (wordAttempts.Count == 0) continue;

            if (!progressMap.TryGetValue(wordId, out var wp))
            {
                wp = new WordProgress
                {
                    StudentId = attempt.StudentId,
                    WordId = wordId,
                    TotalAttempts = 0,
                    TotalCorrect = 0,
                    MasteryScore = 0,
                    LastPracticedAt = null,
                    NextReviewDate = null
                };
                dbContext.WordProgresses.Add(wp);
                progressMap[wordId] = wp;
            }

            wp.TotalAttempts += wordAttempts.Count;
            wp.TotalCorrect += wordAttempts.Count(i => i.WasCorrect);
            wp.LastPracticedAt = DateTime.UtcNow;

            // Fetch the last 10 attempts to calculate consistency (including the ones from the current challenge, which are already committed/saved)
            var recentAttempts = await dbContext.StudentChallengeItemAttempts
                .Where(i => i.StudentChallengeAttempt.StudentId == attempt.StudentId && i.VocabularyItemId == wordId)
                .OrderByDescending(i => i.AnsweredAt)
                .Take(10)
                .Select(i => i.WasCorrect)
                .ToListAsync(cancellationToken);
            recentAttempts.Reverse();

            // Calculate Mastery Score based on new formula
            double accuracy = CalculateAccuracy(wp.TotalCorrect, wp.TotalAttempts);
            double consistency = CalculateConsistency(wp.TotalCorrect, wp.TotalAttempts, recentAttempts);
            double retention = 100.0; // Reset to 100% immediately after practice completion

            wp.MasteryScore = CalculateMasteryScore(accuracy, consistency, retention);

            // Determine if the last attempt in this challenge was correct
            var lastAttempt = wordAttempts.OrderByDescending(i => i.AnsweredAt).First();
            wp.NextReviewDate = CalculateNextReviewDate(wp.MasteryScore, lastAttempt.WasCorrect);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static double CalculateAccuracy(int totalCorrect, int totalAttempts)
    {
        if (totalAttempts <= 0) return 0;
        return Math.Min(100.0, Math.Max(0.0, ((double)totalCorrect / totalAttempts) * 100.0));
    }

    public static double CalculateConsistency(int totalCorrect, int totalAttempts, List<bool>? recentAttempts)
    {
        if (totalAttempts <= 0) return 0;

        double baseAccuracy = CalculateAccuracy(totalCorrect, totalAttempts);

        if (recentAttempts != null && recentAttempts.Count > 0)
        {
            int currentStreak = 0;
            int maxStreak = 0;
            foreach (var wasCorrect in recentAttempts)
            {
                if (wasCorrect)
                {
                    currentStreak++;
                    if (currentStreak > maxStreak) maxStreak = currentStreak;
                }
                else
                {
                    currentStreak = 0;
                }
            }
            double streakConsistency = ((double)maxStreak / Math.Max(1, recentAttempts.Count)) * 100.0;
            return Math.Min(100.0, Math.Max(0.0, (baseAccuracy * 0.6) + (streakConsistency * 0.4)));
        }

        double volumeFactor = Math.Min(1.0, (double)totalAttempts / 8.0);
        return baseAccuracy * volumeFactor;
    }

    public static int CalculateMasteryScore(double accuracy, double consistency, double retention)
    {
        double score = (accuracy * 0.5) + (consistency * 0.3) + (retention * 0.2);
        return Math.Min(100, Math.Max(0, (int)Math.Round(score)));
    }

    public static DateTime CalculateNextReviewDate(int score, bool wasCorrect)
    {
        var now = DateTime.UtcNow;
        if (!wasCorrect || score < 50) return now.AddDays(1); // Weak / Incorrect
        if (score < 80) return now.AddDays(3); // Developing
        return now.AddDays(7); // Mastered
    }

    public async Task<List<StudentWordMasteryDto>> RecordBatchAsync(
        SubmitAdaptiveItemAttemptRequest[] requests,
        CancellationToken cancellationToken)
    {
        if (requests == null || requests.Length == 0)
        {
            return new List<StudentWordMasteryDto>();
        }

        var masteries = new List<StudentWordMasteryDto>();
        var itemAttempts = new List<StudentChallengeItemAttempt>();

        foreach (var request in requests)
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

            itemAttempts.Add(itemAttempt);
        }

        dbContext.StudentChallengeItemAttempts.AddRange(itemAttempts);
        await dbContext.SaveChangesAsync(cancellationToken);

        for (int i = 0; i < requests.Length; i++)
        {
            var request = requests[i];
            var itemAttempt = itemAttempts[i];

            var mastery = await masteryEngine.ApplyItemAttemptAsync(request, cancellationToken);
            if (mastery != null)
            {
                masteries.Add(mastery);
            }

            await LogErrorPatternAsync(request, itemAttempt.Id, cancellationToken);
        }

        return masteries;
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

