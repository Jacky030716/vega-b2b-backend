using System.Text.Json;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Activity;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

public class RecoveryMissionService(
    ApplicationDbContext dbContext,
    IChallengeAiPipelineService challengeAiPipelineService,
    IChallengeOrchestrator challengeOrchestrator,
    IAiAuditService aiAuditService,
    IProgressionRepository progressionRepository,
    ILogger<RecoveryMissionService> logger) : IRecoveryMissionService
{
    private const string RecoverySourceType = "RECOVERY_MISSION";
    private static readonly RecoveryMissionRewardDto DefaultReward = new(50, 2);
    private static readonly string[] DuplicateBlockingStatuses =
    [
        RecoveryMissionStatuses.Pending,
        RecoveryMissionStatuses.Active
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<RecoveryMissionPreviewDto> PreviewAsync(
        int studentId,
        RecoveryMissionPreviewRequest request,
        int teacherId,
        CancellationToken cancellationToken)
    {
        var context = await BuildContextAsync(studentId, request.ClassroomId, request.ModuleId, teacherId, cancellationToken);
        var weakSkill = ResolveWeakSkill(context.WeakRows, request.Mode);
        var gameType = ToGameType(weakSkill);
        var sourceType = request.ModuleId.HasValue
            ? RecoveryMissionSourceTypes.PredefinedModuleRecovery
            : RecoveryMissionSourceTypes.CustomSkillRecovery;

        var targetWords = SelectFallbackWords(context.Vocabulary, context.WeakRows);
        if (targetWords.Count == 0)
            throw new InvalidOperationException("No safe recovery words are available for this student.");

        var generatedBy = RecoveryMissionGeneratedBy.System;
        int? aiAuditLogId = null;
        var reason = BuildFallbackReason(context, weakSkill);
        var difficulty = ResolveDifficulty(context.WeakRows);
        var supportStrategy = BuildSupportStrategy(weakSkill, gameType);

        try
        {
            var moduleTitle = context.ModuleTitle;
            var aiPlan = await challengeAiPipelineService.GenerateModuleChallengePlanAsync(
                new ModuleChallengePlanRequest(
                    request.ModuleId ?? 0,
                    moduleTitle,
                    context.Subject,
                    context.YearLevel,
                    gameType,
                    request.Mode ?? "RECOVERY_MISSION",
                    context.Vocabulary.Select(ToAiItem).ToList(),
                    targetWords,
                    weakSkill,
                    studentId,
                    request.ClassroomId),
                cancellationToken);

            if (aiPlan.Result?.AiAuditLogId is int auditId)
                aiAuditLogId = auditId;

            if (aiPlan.IsSuccess)
            {
                var safeWords = aiPlan.Result.SelectedWords
                    .Where(word => context.Vocabulary.Any(item => MatchesVocabularyWord(item, word)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(7)
                    .ToList();

                if (safeWords.Count >= 3)
                {
                    targetWords = safeWords;
                    gameType = NormalizeGameType(aiPlan.Result.RecommendedGameType);
                    difficulty = Math.Clamp(aiPlan.Result.DifficultyLevel, 1, 3);
                    reason = aiPlan.Result.Reason;
                    generatedBy = RecoveryMissionGeneratedBy.Ai;
                    supportStrategy = BuildSupportStrategy(weakSkill, gameType);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Recovery mission AI preview failed. Falling back to rule-based selection.");
        }

        return new RecoveryMissionPreviewDto(
            BuildTitle(weakSkill, gameType),
            reason,
            weakSkill,
            sourceType,
            targetWords,
            gameType,
            difficulty,
            supportStrategy,
            DefaultReward,
            gameType == "MIXED" ? 7 : 5,
            generatedBy,
            aiAuditLogId,
            JsonSerializer.Serialize(context.TriggerSnapshot, JsonOptions));
    }

    public async Task<RecoveryMissionDto> CreateAsync(
        int studentId,
        CreateRecoveryMissionRequest request,
        int teacherId,
        CancellationToken cancellationToken)
    {
        await CleanupAsync(cancellationToken);
        var preview = request.Preview ?? await PreviewAsync(
            studentId,
            new RecoveryMissionPreviewRequest(request.ClassroomId, request.ModuleId, request.Mode),
            teacherId,
            cancellationToken);

        var context = await BuildContextAsync(studentId, request.ClassroomId, request.ModuleId, teacherId, cancellationToken);
        List<string> safeWords = preview.TargetWords
            .Where(word => context.Vocabulary.Any(item => MatchesVocabularyWord(item, word)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(7)
            .ToList();

        if (safeWords.Count < 3)
            safeWords = SelectFallbackWords(context.Vocabulary, context.WeakRows).ToList();
        if (safeWords.Count < 3)
            throw new InvalidOperationException("Recovery missions require at least 3 validated words.");

        var hasDuplicate = await dbContext.RecoveryMissions.AsNoTracking()
            .AnyAsync(mission =>
                mission.StudentId == studentId &&
                mission.ClassroomId == request.ClassroomId &&
                mission.ModuleId == request.ModuleId &&
                mission.WeakSkill == preview.WeakSkill &&
                DuplicateBlockingStatuses.Contains(mission.Status),
                cancellationToken);
        if (hasDuplicate)
            throw new DuplicateRecoveryMissionException("An active recovery mission already exists for this skill.");

        var selectedItems = SelectVocabularyItems(context.Vocabulary, safeWords);
        var config = await challengeAiPipelineService.GenerateGameConfigAsync(
            new GameConfigGenerationRequest(
                request.ModuleId ?? 0,
                context.ModuleTitle,
                context.Subject,
                request.ClassroomId,
                "RECOVERY_MISSION",
                RecoverySourceType,
                preview.RecommendedGameType,
                preview.DifficultyLevel,
                selectedItems.Select(ToAdaptiveItem).ToList()),
            cancellationToken);
        if (!config.IsSuccess)
            throw new InvalidOperationException(config.ErrorMessage ?? "Unable to create recovery mission content.");

        var assigned = await challengeOrchestrator.AssignAsync(
            new AssignAdaptiveChallengeRequest(
                teacherId,
                studentId,
                request.ClassroomId,
                DateTime.UtcNow.AddDays(7),
                config.Result with
                {
                    Title = preview.Title,
                    Description = preview.Reason,
                    SourceType = RecoverySourceType,
                    ModuleId = request.ModuleId
                },
                context.Subject,
                null,
                preview.GeneratedBy == RecoveryMissionGeneratedBy.Ai
                    ? AiGenerationStatuses.AiAssisted
                    : AiGenerationStatuses.FailedFallback,
                AiUseCases.RecoveryMissionPreview,
                preview.AiAuditLogId),
            cancellationToken);

        if (preview.AiAuditLogId is int auditLogId)
            await aiAuditService.AttachChallengeAsync(auditLogId, assigned.ChallengeId, cancellationToken);

        var mission = new RecoveryMission
        {
            StudentId = studentId,
            ClassroomId = request.ClassroomId,
            ModuleId = request.ModuleId,
            SourceType = preview.SourceType,
            Title = preview.Title,
            Reason = preview.Reason,
            RecommendedGameType = preview.RecommendedGameType,
            DifficultyLevel = preview.DifficultyLevel,
            TargetWordsJson = JsonSerializer.Serialize(safeWords, JsonOptions),
            ConfigJson = JsonSerializer.Serialize(new
            {
                preview.SupportStrategy,
                preview.EstimatedMinutes,
                linkedChallengeId = assigned.ChallengeId
            }, JsonOptions),
            RewardJson = JsonSerializer.Serialize(preview.Reward, JsonOptions),
            Status = RecoveryMissionStatuses.Active,
            GeneratedBy = preview.GeneratedBy,
            ApprovedByTeacherId = teacherId,
            AiAuditLogId = preview.AiAuditLogId,
            AvailableUntil = DateTime.UtcNow.AddDays(7),
            LinkedChallengeId = assigned.ChallengeId,
            WeakSkill = preview.WeakSkill,
            TriggerSnapshotJson = preview.TriggerSnapshotJson
        };

        dbContext.RecoveryMissions.Add(mission);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.ActivityLogs.Add(new ActivityLog
        {
            UserId = studentId,
            Type = "recovery_mission",
            Title = "New Recovery Mission",
            Description = preview.Title,
            ReferenceId = $"recovery-mission:{mission.Id}"
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(mission, assigned.GameKey, config.Result.ContentData);
    }

    public async Task<IReadOnlyList<RecoveryMissionDto>> GetForTeacherAsync(int studentId, int teacherId, CancellationToken cancellationToken)
    {
        await CleanupAsync(cancellationToken);
        var classroomIds = await dbContext.Classrooms.AsNoTracking()
            .Where(c => c.TeacherId == teacherId && c.IsActive && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var missions = await QueryMissions()
            .Where(m => m.StudentId == studentId && classroomIds.Contains(m.ClassroomId))
            .OrderByDescending(m => m.CreatedTime)
            .ToListAsync(cancellationToken);

        return missions.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<RecoveryMissionDto>> GetActiveForStudentAsync(int studentId, CancellationToken cancellationToken)
    {
        await CleanupAsync(cancellationToken);
        var visibleStatuses = new[]
        {
            RecoveryMissionStatuses.Active,
            RecoveryMissionStatuses.Completed
        };
        var now = DateTime.UtcNow;
        var missions = await QueryMissions()
            .Where(m => m.StudentId == studentId
                        && visibleStatuses.Contains(m.Status)
                        && (m.Status != RecoveryMissionStatuses.Completed || m.ArchiveAt == null || m.ArchiveAt > now))
            .OrderBy(m => m.Status == RecoveryMissionStatuses.Active ? 0 : 1)
            .ThenByDescending(m => m.CreatedTime)
            .ToListAsync(cancellationToken);

        return missions.Select(ToDto).ToList();
    }

    public async Task<RecoveryMissionStartDto> StartAsync(int missionId, int studentId, CancellationToken cancellationToken)
    {
        await CleanupAsync(cancellationToken);
        var mission = await QueryMissions()
            .FirstOrDefaultAsync(m => m.Id == missionId && m.StudentId == studentId, cancellationToken)
            ?? throw new InvalidOperationException("Recovery mission not found");

        if (mission.Status != RecoveryMissionStatuses.Active)
            throw new InvalidOperationException("Recovery mission is not active");
        if (mission.LinkedChallenge is null)
            throw new InvalidOperationException("Recovery mission content is unavailable");

        return new RecoveryMissionStartDto(
            ToDto(mission),
            mission.Id,
            mission.LinkedChallenge.Id,
            mission.LinkedChallenge.Game?.Key ?? ChallengeGenerator.ToGameKey(mission.RecommendedGameType),
            mission.LinkedChallenge.Title,
            mission.LinkedChallenge.Description,
            mission.LinkedChallenge.DifficultyLevel,
            mission.LinkedChallenge.ContentData);
    }

    public async Task<RecoveryMissionCompleteDto> CompleteAsync(int missionId, int studentId, CancellationToken cancellationToken)
    {
        var mission = await dbContext.RecoveryMissions
            .FirstOrDefaultAsync(m => m.Id == missionId && m.StudentId == studentId, cancellationToken)
            ?? throw new InvalidOperationException("Recovery mission not found");

        if (mission.Status == RecoveryMissionStatuses.Archived)
            throw new InvalidOperationException("Recovery mission is archived");

        var reward = ReadReward(mission.RewardJson);
        var xp = 0;
        var diamonds = 0;
        if (!mission.RewardClaimed)
        {
            xp = reward.Xp;
            diamonds = reward.Diamonds;
            await progressionRepository.AddXpAsync(studentId, xp);
            await progressionRepository.AddDiamondsAsync(studentId, diamonds);
            mission.RewardClaimed = true;
        }

        mission.Status = RecoveryMissionStatuses.Completed;
        mission.CompletedAt ??= DateTime.UtcNow;
        mission.ArchiveAt ??= DateTime.UtcNow.AddHours(24);

        if (mission.ApprovedByTeacherId is int teacherId)
        {
            dbContext.ActivityLogs.Add(new ActivityLog
            {
                UserId = teacherId,
                Type = "recovery_mission_completed",
                Title = "Recovery Mission Completed",
                Description = mission.Title,
                PointsEarned = xp,
                ReferenceId = $"recovery-mission:{mission.Id}"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RecoveryMissionCompleteDto(true, xp, diamonds, mission.ArchiveAt.Value, ToDto(mission));
    }

    private IQueryable<RecoveryMission> QueryMissions()
        => dbContext.RecoveryMissions.AsNoTracking()
            .Include(m => m.LinkedChallenge)!.ThenInclude(c => c!.Game);

    private async Task<RecoveryContext> BuildContextAsync(
        int studentId,
        int classroomId,
        int? moduleId,
        int teacherId,
        CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classroomId && c.IsActive && !c.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found");
        if (classroom.TeacherId != teacherId)
            throw new UnauthorizedAccessException("You do not manage this classroom");

        var isMember = await dbContext.ClassroomStudents.AsNoTracking()
            .AnyAsync(s => s.ClassroomId == classroomId && s.UserId == studentId, cancellationToken);
        if (!isMember)
            throw new InvalidOperationException("Student is not in this classroom");

        if (moduleId.HasValue)
        {
            var isAttached = await dbContext.ClassroomModules.AsNoTracking()
                .AnyAsync(link => link.ClassroomId == classroomId && link.ModuleId == moduleId.Value, cancellationToken);
            if (!isAttached)
                throw new InvalidOperationException("Module is not attached to this classroom");
        }

        var vocabularyQuery = dbContext.VocabularyItems.AsNoTracking().Where(v => v.IsActive);
        if (moduleId.HasValue)
            vocabularyQuery = vocabularyQuery.Where(v => v.ModuleId == moduleId.Value);
        else
            vocabularyQuery = vocabularyQuery.Where(v => v.YearLevel == classroom.YearLevel && v.Subject == classroom.Subject);

        var vocabulary = await vocabularyQuery
            .OrderBy(v => v.DisplayOrder)
            .ThenBy(v => v.Word)
            .Take(80)
            .ToListAsync(cancellationToken);

        if (vocabulary.Count == 0)
            throw new InvalidOperationException("No vocabulary is available for recovery mission generation");

        var vocabularyIds = vocabulary.Select(v => v.Id).ToArray();
        var weakRows = await dbContext.StudentWordMasteries.AsNoTracking()
            .Include(m => m.VocabularyItem)
            .Where(m => m.StudentId == studentId && vocabularyIds.Contains(m.VocabularyItemId))
            .OrderBy(m => m.MasteryScore)
            .ThenByDescending(m => m.TotalHintsUsed)
            .ThenByDescending(m => m.TotalRetries)
            .Take(20)
            .ToListAsync(cancellationToken);

        var module = moduleId.HasValue
            ? await dbContext.SyllabusModules.AsNoTracking().FirstOrDefaultAsync(m => m.Id == moduleId.Value, cancellationToken)
            : null;

        var challengeIds = await dbContext.Challenges.AsNoTracking()
            .Where(c => c.ClassroomId == classroomId && (!moduleId.HasValue || c.ModuleId == moduleId.Value))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        var failedAttemptCount = await dbContext.ChallengeProgresses.AsNoTracking()
            .Where(p => p.UserId == studentId && challengeIds.Contains(p.ChallengeId) && p.BestAccuracy < 50)
            .CountAsync(cancellationToken);

        var overdueCount = weakRows.Count(row => row.NextReviewAt.HasValue && row.NextReviewAt.Value <= DateTime.UtcNow);
        var triggerSnapshot = new
        {
            weakWordCount = weakRows.Count(row => row.MasteryScore < 40),
            overdueReviewCount = overdueCount,
            failedChallengeCount = failedAttemptCount,
            excessiveHintsCount = weakRows.Count(row => row.TotalHintsUsed >= 3),
            generatedAt = DateTime.UtcNow
        };

        return new RecoveryContext(
            classroom.Subject,
            classroom.YearLevel,
            module is null ? classroom.Name : string.IsNullOrWhiteSpace(module.UnitTitle) ? module.Title : module.UnitTitle,
            vocabulary,
            weakRows,
            triggerSnapshot);
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rows = await dbContext.RecoveryMissions
            .Where(m => (m.Status == RecoveryMissionStatuses.Active || m.Status == RecoveryMissionStatuses.Pending)
                            && m.AvailableUntil != null && m.AvailableUntil < now
                        || m.Status == RecoveryMissionStatuses.Completed
                            && m.ArchiveAt != null && m.ArchiveAt < now)
            .ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            row.Status = row.Status == RecoveryMissionStatuses.Completed
                ? RecoveryMissionStatuses.Archived
                : RecoveryMissionStatuses.Expired;
        }

        if (rows.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static RecoveryMissionDto ToDto(RecoveryMission mission)
        => ToDto(
            mission,
            mission.LinkedChallenge?.Game?.Key,
            mission.LinkedChallenge?.ContentData);

    private static RecoveryMissionDto ToDto(RecoveryMission mission, string? gameKey, string? contentData)
    {
        var config = ReadConfig(mission.ConfigJson);
        return new RecoveryMissionDto(
            mission.Id,
            mission.StudentId,
            mission.ClassroomId,
            mission.ModuleId,
            mission.Title,
            mission.Reason,
            mission.WeakSkill,
            mission.SourceType,
            ReadWords(mission.TargetWordsJson),
            mission.RecommendedGameType,
            mission.DifficultyLevel,
            mission.Status,
            ReadReward(mission.RewardJson),
            mission.AvailableUntil,
            mission.CompletedAt,
            mission.ArchiveAt,
            mission.LinkedChallengeId,
            gameKey,
            contentData,
            config);
    }

    private static IReadOnlyList<string> SelectFallbackWords(
        IReadOnlyList<VocabularyItem> vocabulary,
        IReadOnlyList<StudentWordMastery> weakRows)
    {
        var weakWords = weakRows
            .Where(row => row.MasteryScore < 50 || row.TotalRetries >= 3 || row.TotalHintsUsed >= 3 || row.NextReviewAt <= DateTime.UtcNow)
            .Select(row => row.VocabularyItem.Word)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(7)
            .ToList();

        if (weakWords.Count >= 3)
            return weakWords;

        foreach (var word in vocabulary.Select(v => v.Word).Where(word => !string.IsNullOrWhiteSpace(word)))
        {
            if (weakWords.Count >= 3) break;
            if (!weakWords.Contains(word, StringComparer.OrdinalIgnoreCase))
                weakWords.Add(word);
        }

        return weakWords.Take(7).ToList();
    }

    private static IReadOnlyList<VocabularyItem> SelectVocabularyItems(
        IReadOnlyList<VocabularyItem> vocabulary,
        IReadOnlyList<string> selectedWords)
    {
        var selected = new List<VocabularyItem>();
        foreach (var word in selectedWords)
        {
            var item = vocabulary.FirstOrDefault(v => MatchesVocabularyWord(v, word));
            if (item is not null && selected.All(existing => existing.Id != item.Id))
                selected.Add(item);
        }

        return selected;
    }

    private static string ResolveWeakSkill(IReadOnlyList<StudentWordMastery> rows, string? mode)
    {
        var source = string.Join(" ", rows.Select(row => row.WeaknessTagsJson)) + " " + mode;
        if (source.Contains("syllable", StringComparison.OrdinalIgnoreCase)) return "SYLLABLE_STRUCTURE";
        if (source.Contains("speak", StringComparison.OrdinalIgnoreCase) || source.Contains("voice", StringComparison.OrdinalIgnoreCase)) return "SPEAKING";
        if (source.Contains("listen", StringComparison.OrdinalIgnoreCase)) return "LISTENING";
        if (source.Contains("mixed", StringComparison.OrdinalIgnoreCase)) return "MIXED";
        return "SPELLING_RECALL";
    }

    private static string ToGameType(string weakSkill)
        => weakSkill switch
        {
            "SYLLABLE_STRUCTURE" => "SYLLABLE_SUSHI",
            "SPEAKING" => "VOICE_BRIDGE",
            "MIXED" => "SYLLABLE_SUSHI",
            _ => "SPELL_CATCHER"
        };

    private static int ResolveDifficulty(IReadOnlyList<StudentWordMastery> rows)
        => rows.Any(row => row.MasteryScore < 25) ? 1 : rows.Any(row => row.MasteryScore < 50) ? 2 : 1;

    private static string BuildFallbackReason(RecoveryContext context, string weakSkill)
    {
        var weakCount = context.WeakRows.Count(row => row.MasteryScore < 50);
        var overdueCount = context.WeakRows.Count(row => row.NextReviewAt <= DateTime.UtcNow);
        if (overdueCount > 0)
            return $"{overdueCount} review word{(overdueCount == 1 ? "" : "s")} are overdue and need a short recovery mission.";
        if (weakCount > 0)
            return $"{weakCount} word{(weakCount == 1 ? "" : "s")} show repeated weakness in {weakSkill.Replace('_', ' ').ToLowerInvariant()}.";
        return "This student needs a short targeted recovery mission to rebuild confidence.";
    }

    private static string BuildTitle(string weakSkill, string gameType)
        => weakSkill switch
        {
            "SYLLABLE_STRUCTURE" => "Syllable Recovery Mission",
            "SPEAKING" => "Speaking Recovery Mission",
            "MIXED" => "Mixed Skills Recovery Mission",
            _ => gameType == "SPELL_CATCHER" ? "Spelling Recovery Mission" : "Recovery Mission"
        };

    private static string BuildSupportStrategy(string weakSkill, string gameType)
        => $"Focus on {weakSkill.Replace('_', ' ').ToLowerInvariant()} using {gameType.Replace('_', ' ')} with a short word set.";

    private static string NormalizeGameType(string? gameType)
    {
        var normalized = gameType?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized switch
        {
            "SPELL_CATCHER" or "SPELL" => "SPELL_CATCHER",
            "SYLLABLE_SUSHI" or "SYLLABLE" => "SYLLABLE_SUSHI",
            "VOICE_BRIDGE" or "VOICE" or "SPEAKING" => "VOICE_BRIDGE",
            _ => "SPELL_CATCHER"
        };
    }

    private static ModuleChallengeAiItem ToAiItem(VocabularyItem item) =>
        new(item.Id, item.Word, item.BmText, item.EnText, item.ZhText, item.SyllablesJson, item.SyllableText, item.ItemType, item.DifficultyLevel, item.MeaningText, item.ExampleSentence);

    private static AdaptiveChallengeItemDto ToAdaptiveItem(VocabularyItem item) =>
        new(null, item.Id, item.Word, item.NormalizedWord, item.PhoneticHint ?? item.MeaningText, item.MeaningText, item.ExampleSentence, item.SyllablesJson, item.DifficultyLevel, item.BmText, item.ZhText, item.EnText, item.SyllableText, item.ItemType, item.DisplayOrder);

    private static bool MatchesVocabularyWord(VocabularyItem item, string selectedWord)
        => string.Equals(item.Word, selectedWord, StringComparison.OrdinalIgnoreCase)
           || string.Equals(item.BmText, selectedWord, StringComparison.OrdinalIgnoreCase)
           || string.Equals(item.EnText, selectedWord, StringComparison.OrdinalIgnoreCase)
           || string.Equals(item.ZhText, selectedWord, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> ReadWords(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? []; }
        catch { return []; }
    }

    private static RecoveryMissionRewardDto ReadReward(string json)
    {
        try { return JsonSerializer.Deserialize<RecoveryMissionRewardDto>(json, JsonOptions) ?? DefaultReward; }
        catch { return DefaultReward; }
    }

    private static int ReadConfig(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("estimatedMinutes", out var minutes) && minutes.TryGetInt32(out var value))
                return value;
        }
        catch
        {
        }

        return 5;
    }

    private sealed record RecoveryContext(
        string Subject,
        int YearLevel,
        string ModuleTitle,
        IReadOnlyList<VocabularyItem> Vocabulary,
        IReadOnlyList<StudentWordMastery> WeakRows,
        object TriggerSnapshot);
}

public class DuplicateRecoveryMissionException(string message) : InvalidOperationException(message);
