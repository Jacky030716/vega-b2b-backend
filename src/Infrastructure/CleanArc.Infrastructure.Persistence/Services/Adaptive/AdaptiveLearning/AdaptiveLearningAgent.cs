using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CleanArc.Application.Contracts.AdaptiveLearning;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive.AdaptiveLearning;

public class AdaptiveLearningAgent : IAdaptiveLearningAgent
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEnumerable<IChallengeGenerator> _generators;
    private readonly HardcoreRewardConfig _rewardConfig;
    private readonly ILogger<AdaptiveLearningAgent> _logger;

    public AdaptiveLearningAgent(
        ApplicationDbContext dbContext,
        IEnumerable<IChallengeGenerator> generators,
        ILogger<AdaptiveLearningAgent> logger)
    {
        _dbContext = dbContext;
        _generators = generators;
        _logger = logger;
        _rewardConfig = new HardcoreRewardConfig(); // Centralized configuration defaults
    }

    public async Task EvaluateAndTriggerDraftAsync(
        int studentId,
        int triggeringAttemptId,
        bool isSpellingTest,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("AdaptiveLearningAgent: Starting evaluation for student {StudentId}", studentId);

        try
        {
            // 1. Gather recent completed attempts (standard + spelling test attempts)
            var unifiedAttempts = await GetRecentAttemptsAsync(studentId, cancellationToken);

            if (unifiedAttempts.Count < 3)
            {
                var notEnoughReason = $"Student only has {unifiedAttempts.Count} completed attempts (minimum 3 required).";
                await SaveDecisionAsync(studentId, false, notEnoughReason, 0.0, "{}", null, cancellationToken);
                return;
            }

            // 2. Evaluate metrics on the last 3 attempts
            var evaluatedList = new List<AttemptMetricEvaluation>();
            bool isEligible = true;
            string decisionReason = "Student consistently performed above the expected mastery threshold.";

            foreach (var att in unifiedAttempts.Take(3))
            {
                var evaluation = EvaluateAttempt(att);
                evaluatedList.Add(evaluation);

                if (!evaluation.IsHighPerforming)
                {
                    isEligible = false;
                    decisionReason = $"Student is not eligible because attempt {att.Id} (Type: {att.Type}) did not meet high-performance criteria. " +
                                     $"Score: {evaluation.Score}% (Min: 90%), Hints: {evaluation.HintsUsed} (Max: 0), Retries: {evaluation.RetriesCount} (Max: 0), Speed: {(evaluation.IsFast ? "Fast" : "Slow")}.";
                }
            }

            var triggeringMetricsJson = JsonSerializer.Serialize(evaluatedList);

            if (!isEligible)
            {
                _logger.LogInformation("AdaptiveLearningAgent: Student {StudentId} is not eligible. Reason: {Reason}", studentId, decisionReason);
                await SaveDecisionAsync(studentId, false, decisionReason, 0.0, triggeringMetricsJson, null, cancellationToken);
                return;
            }

            // 3. Student is eligible! Prepare Hardcore Challenge Draft.
            _logger.LogInformation("AdaptiveLearningAgent: Student {StudentId} is eligible for a hardcore challenge!", studentId);

            // Determine target words from the triggering attempt
            var words = await ExtractTargetWordsAsync(triggeringAttemptId, isSpellingTest, cancellationToken);
            if (words.Count == 0)
            {
                var noWordsReason = "Eligible, but could not retrieve target words from the triggering attempt.";
                await SaveDecisionAsync(studentId, false, noWordsReason, 0.0, triggeringMetricsJson, null, cancellationToken);
                return;
            }

            // Select a game type randomly from the four supported types
            var gameTypes = new[] { "VOICE_BRIDGE", "SYLLABLE_SUSHI", "SPELL_CATCHER", "SPELLING_TEST" };
            var random = new Random();
            var chosenGameType = gameTypes[random.Next(gameTypes.Length)];

            var generator = _generators.FirstOrDefault(g => g.GameType.Equals(chosenGameType, StringComparison.OrdinalIgnoreCase));
            if (generator == null)
            {
                var noGenReason = $"Challenge generator for game type {chosenGameType} is not registered.";
                await SaveDecisionAsync(studentId, false, noGenReason, 0.0, triggeringMetricsJson, null, cancellationToken);
                return;
            }

            // Generate content
            var contentData = await generator.GenerateContentJsonAsync(words, 5, cancellationToken);

            // Determine Mascot reward if eligible
            bool awardMascot = random.NextDouble() < _rewardConfig.MascotProbability;
            string? chosenMascot = awardMascot ? _rewardConfig.MascotNames[random.Next(_rewardConfig.MascotNames.Count)] : null;

            var title = $"Professor Vega Hardcore Challenge: {chosenGameType.Replace('_', ' ')}";
            var description = $"An exclusive elite difficulty practice designed specifically for your exceptional performance in spelling and oral recall.";

            var draft = new HardcoreChallengeDraft
            {
                StudentId = studentId,
                Title = title,
                Description = description,
                GameType = chosenGameType,
                DifficultyLevel = 5,
                TargetWordsJson = JsonSerializer.Serialize(words),
                ContentData = contentData,
                RewardXp = _rewardConfig.DefaultBonusXp,
                RewardDiamonds = _rewardConfig.DefaultBonusDiamonds,
                MascotEligibility = awardMascot,
                MascotName = chosenMascot,
                BadgeCode = _rewardConfig.BadgeCode,
                Status = "PENDING",
                ExpiryAt = DateTime.UtcNow.AddHours(_rewardConfig.ExpiryDurationHours),
                DecisionReason = decisionReason,
                ConfidenceScore = 1.0,
                TriggeringMetricsJson = triggeringMetricsJson
            };

            _dbContext.HardcoreChallengeDrafts.Add(draft);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Save decision linking the generated draft
            await SaveDecisionAsync(studentId, true, decisionReason, 1.0, triggeringMetricsJson, draft.Id, cancellationToken);
            _logger.LogInformation("AdaptiveLearningAgent: Hardcore challenge draft {DraftId} successfully created for student {StudentId}", draft.Id, studentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdaptiveLearningAgent: Error during evaluation for student {StudentId}", studentId);
        }
    }

    private async Task<List<UnifiedAttempt>> GetRecentAttemptsAsync(int studentId, CancellationToken cancellationToken)
    {
        var list = new List<UnifiedAttempt>();

        // Fetch normal challenge attempts
        var challengeAttempts = await _dbContext.Attempts
            .AsNoTracking()
            .Include(a => a.Challenge)
            .Where(a => a.UserId == studentId && a.IsCompleted)
            .OrderByDescending(a => a.CompletedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var a in challengeAttempts)
        {
            list.Add(new UnifiedAttempt
            {
                Id = a.Id,
                Type = "CHALLENGE",
                CompletedAt = a.CompletedAt,
                Score = a.Score,
                AttemptData = a.AttemptData,
                ContentData = a.Challenge?.ContentData
            });
        }

        // Fetch spelling test attempts
        var testAttempts = await _dbContext.StudentSpellingTestAttempts
            .AsNoTracking()
            .Include(sa => sa.SpellingTest)
            .Where(sa => sa.StudentId == studentId && sa.Status == "COMPLETED" && sa.CompletedAt.HasValue)
            .OrderByDescending(sa => sa.CompletedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var sa in testAttempts)
        {
            list.Add(new UnifiedAttempt
            {
                Id = sa.Id,
                Type = "SPELLING_TEST",
                CompletedAt = sa.CompletedAt!.Value,
                Score = sa.Score ?? 0,
                AttemptData = sa.ResultJson,
                ContentData = null // spelling test target words are fetched via IDs in WordItemIdsJson
            });
        }

        return list.OrderByDescending(a => a.CompletedAt).ToList();
    }

    private AttemptMetricEvaluation EvaluateAttempt(UnifiedAttempt att)
    {
        int score = att.Score;
        int hintsUsed = 0;
        int retriesCount = 0;
        double durationSeconds = 0;
        int wordCount = 8; // Default fallback

        try
        {
            if (!string.IsNullOrWhiteSpace(att.AttemptData))
            {
                using var doc = JsonDocument.Parse(att.AttemptData);
                var root = doc.RootElement;

                if (att.Type == "CHALLENGE")
                {
                    // Parse duration
                    var duration = TryReadDouble(root, "durationSeconds")
                        ?? TryReadDouble(root, "timeTakenSeconds")
                        ?? TryReadDouble(root, "duration");
                    durationSeconds = duration ?? 0;

                    // Parse hints and retries from results array
                    if (root.TryGetProperty("results", out var resultsProp) && resultsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in resultsProp.EnumerateArray())
                        {
                            hintsUsed += elem.TryGetProperty("hintsUsed", out var h) && h.ValueKind == JsonValueKind.Number ? h.GetInt32() : 0;
                            retriesCount += elem.TryGetProperty("retriesCount", out var r) && r.ValueKind == JsonValueKind.Number ? r.GetInt32() : 0;
                        }
                    }

                    // Parse word count from Challenge ContentData
                    if (!string.IsNullOrWhiteSpace(att.ContentData))
                    {
                        using var contentDoc = JsonDocument.Parse(att.ContentData);
                        if (contentDoc.RootElement.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
                        {
                            wordCount = Math.Max(itemsProp.GetArrayLength(), 1);
                        }
                    }
                }
                else if (att.Type == "SPELLING_TEST")
                {
                    // Parse spelling test result envelope
                    if (root.TryGetProperty("Results", out var resultsProp) && resultsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in resultsProp.EnumerateArray())
                        {
                            hintsUsed += elem.TryGetProperty("HintsUsed", out var h) && h.ValueKind == JsonValueKind.Number ? h.GetInt32() : 0;
                            retriesCount += elem.TryGetProperty("RetriesCount", out var r) && r.ValueKind == JsonValueKind.Number ? r.GetInt32() : 0;
                            var respTime = elem.TryGetProperty("ResponseTimeMs", out var rt) && rt.ValueKind == JsonValueKind.Number ? rt.GetDouble() : 0;
                            durationSeconds += respTime / 1000.0;
                        }
                        wordCount = Math.Max(resultsProp.GetArrayLength(), 1);
                    }
                }
            }
        }
        catch
        {
            // Fallback to defaults if telemetry parsing fails
        }

        // Expected speed: less than 6 seconds per word
        bool isFast = durationSeconds > 0 && durationSeconds < (wordCount * 6.0);
        bool isHighPerforming = score >= 90 && hintsUsed == 0 && retriesCount == 0 && isFast;

        return new AttemptMetricEvaluation(att.Id, att.Type, score, hintsUsed, retriesCount, durationSeconds, wordCount, isFast, isHighPerforming);
    }

    private async Task<List<string>> ExtractTargetWordsAsync(int attemptId, bool isSpellingTest, CancellationToken cancellationToken)
    {
        var words = new List<string>();

        if (isSpellingTest)
        {
            var attempt = await _dbContext.StudentSpellingTestAttempts
                .AsNoTracking()
                .Include(sa => sa.SpellingTest)
                .FirstOrDefaultAsync(sa => sa.Id == attemptId, cancellationToken);

            if (attempt?.SpellingTest != null)
            {
                var wordIds = JsonSerializer.Deserialize<List<int>>(attempt.SpellingTest.WordItemIdsJson ?? "[]");
                if (wordIds?.Count > 0)
                {
                    var items = await _dbContext.VocabularyItems
                        .AsNoTracking()
                        .Where(v => wordIds.Contains(v.Id) && v.IsActive)
                        .Select(v => v.Word)
                        .ToListAsync(cancellationToken);
                    words.AddRange(items);
                }
            }
        }
        else
        {
            var attempt = await _dbContext.Attempts
                .AsNoTracking()
                .Include(a => a.Challenge)
                .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);

            if (attempt?.Challenge != null && !string.IsNullOrWhiteSpace(attempt.Challenge.ContentData))
            {
                try
                {
                    using var doc = JsonDocument.Parse(attempt.Challenge.ContentData);
                    if (doc.RootElement.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in itemsProp.EnumerateArray())
                        {
                            if (elem.TryGetProperty("word", out var wProp))
                            {
                                words.Add(wProp.GetString() ?? string.Empty);
                            }
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        return words.Where(w => !string.IsNullOrWhiteSpace(w)).Distinct().ToList();
    }

    private async Task SaveDecisionAsync(
        int studentId,
        bool isEligible,
        string reason,
        double confidence,
        string metricsJson,
        int? draftId,
        CancellationToken cancellationToken)
    {
        var decision = new AdaptiveAgentDecision
        {
            AgentName = "AdaptiveLearningAgent",
            StudentId = studentId,
            EvaluatedAt = DateTime.UtcNow,
            IsEligible = isEligible,
            DecisionReason = reason,
            ConfidenceScore = confidence,
            TriggeringMetricsJson = metricsJson,
            GeneratedDraftId = draftId
        };

        _dbContext.AdaptiveAgentDecisions.Add(decision);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static double? TryReadDouble(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var val)) return null;
        if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var num)) return num;
        if (val.ValueKind == JsonValueKind.String && double.TryParse(val.GetString(), out var parsed)) return parsed;
        return null;
    }

    private class UnifiedAttempt
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // CHALLENGE or SPELLING_TEST
        public DateTime CompletedAt { get; set; }
        public int Score { get; set; }
        public string AttemptData { get; set; } = string.Empty;
        public string? ContentData { get; set; }
    }

    private record AttemptMetricEvaluation(
        int AttemptId,
        string Type,
        int Score,
        int HintsUsed,
        int RetriesCount,
        double DurationSeconds,
        int WordCount,
        bool IsFast,
        bool IsHighPerforming);
}
