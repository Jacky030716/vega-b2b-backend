using System.Text.Json;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

#nullable enable

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

public class ClassroomModuleManagementService(
    ApplicationDbContext dbContext,
    IChallengeOrchestrator challengeOrchestrator,
    IChallengeAiPipelineService challengeAiPipelineService,
    IAiAuditService aiAuditService,
    ILogger<ClassroomModuleManagementService> logger) : IClassroomModuleManagementService
{
    private static readonly HashSet<ChallengeLifecycleState> ActiveStates = new()
    {
        ChallengeLifecycleState.Active,
        ChallengeLifecycleState.Scheduled
    };
    private const string RecoverySourceType = "RECOVERY_MISSION";
    private const int MaxGameChallengesPerModule = 3;

    public async Task<ClassroomModuleOverviewDto> GetModuleOverviewAsync(int classroomId, int teacherId, CancellationToken cancellationToken, bool isAdmin = false)
    {
        var classroom = await GetTeacherClassroomAsync(classroomId, teacherId, cancellationToken, isAdmin);
        await EnsureClassroomModuleLinksAsync(classroom, teacherId, cancellationToken);
        var customModule = await EnsureCustomModuleAsync(classroom, teacherId, cancellationToken);
        var studentCount = await dbContext.ClassroomStudents.CountAsync(s => s.ClassroomId == classroomId, cancellationToken);
        var studentIds = await dbContext.ClassroomStudents.AsNoTracking()
            .Where(s => s.ClassroomId == classroomId)
            .Select(s => s.UserId)
            .ToListAsync(cancellationToken);

        var modules = await dbContext.ClassroomModules.AsNoTracking()
            .Include(link => link.Module)
            .Where(link => link.ClassroomId == classroomId
                           && link.Module.IsActive
                           && link.Module.ModuleType == SyllabusModule.PredefinedModuleType)
            .Select(link => link.Module)
            .OrderBy(m => m.Subject)
            .ThenBy(m => m.UnitNumber ?? int.MaxValue)
            .ThenBy(m => m.Title)
            .ToListAsync(cancellationToken);

        var moduleIds = modules.Select(m => m.Id).ToArray();
        var vocabularyCounts = await dbContext.VocabularyItems.AsNoTracking()
            .Where(v => moduleIds.Contains(v.ModuleId) && v.IsActive)
            .GroupBy(v => v.ModuleId)
            .Select(g => new { ModuleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ModuleId, x => x.Count, cancellationToken);

        var challengeCounts = await dbContext.Challenges.AsNoTracking()
            .Where(c => c.ClassroomId == classroomId && c.ModuleId != null && moduleIds.Contains(c.ModuleId.Value) && (c.SourceType == null || c.SourceType != RecoverySourceType))
            .GroupBy(c => c.ModuleId!.Value)
            .Select(g => new
            {
                ModuleId = g.Key,
                Generated = g.Count(),
                Active = g.Count(c => c.LifecycleState == ChallengeLifecycleState.Active || c.LifecycleState == ChallengeLifecycleState.Scheduled),
                LastActivityAt = g.Max(c => c.LastActivityAt ?? c.ModifiedDate ?? c.CreatedTime)
            })
            .ToDictionaryAsync(x => x.ModuleId, x => x, cancellationToken);

        var progress = await dbContext.Challenges.AsNoTracking()
            .Where(c =>
                c.ClassroomId == classroomId &&
                c.ModuleId != null &&
                moduleIds.Contains(c.ModuleId.Value) &&
                (c.SourceType == null || c.SourceType != RecoverySourceType))
            .Select(c => new
            {
                ModuleId = c.ModuleId!.Value,
                IsCompleted =
                    c.LifecycleState == ChallengeLifecycleState.Completed ||
                    dbContext.ChallengeProgresses.AsNoTracking().Any(progress =>
                        progress.ClassroomId == classroomId &&
                        progress.ChallengeId == c.Id &&
                        progress.HasCompleted)
            })
            .GroupBy(x => x.ModuleId)
            .Select(g => new
            {
                ModuleId = g.Key,
                Completed = g.Count(x => x.IsCompleted),
                Progress = g.Any() ? (int)Math.Round((double)g.Count(x => x.IsCompleted) / g.Count() * 100) : 0
            })
            .ToDictionaryAsync(x => x.ModuleId, x => x, cancellationToken);

        var masteryStats = await dbContext.StudentWordMasteries.AsNoTracking()
            .Where(m => m.ModuleId != null && moduleIds.Contains(m.ModuleId.Value) && studentIds.Contains(m.StudentId))
            .GroupBy(m => m.ModuleId!.Value)
            .Select(g => new
            {
                ModuleId = g.Key,
                Weak = g.Count(x => x.MasteryScore < 65),
                AverageScore = Math.Round(g.Average(x => (decimal)x.MasteryScore), 2)
            })
            .ToDictionaryAsync(x => x.ModuleId, x => x, cancellationToken);

        var practicedFromMastery = await dbContext.StudentWordMasteries.AsNoTracking()
            .Where(m => m.ModuleId != null && moduleIds.Contains(m.ModuleId.Value) && studentIds.Contains(m.StudentId))
            .Select(m => new { ModuleId = m.ModuleId!.Value, m.VocabularyItemId })
            .ToListAsync(cancellationToken);

        var practicedFromItemAttempts = await dbContext.StudentChallengeItemAttempts.AsNoTracking()
            .Where(item => item.VocabularyItemId != null
                           && studentIds.Contains(item.StudentChallengeAttempt.StudentId)
                           && item.VocabularyItem != null
                           && moduleIds.Contains(item.VocabularyItem.ModuleId))
            .Select(item => new
            {
                ModuleId = item.VocabularyItem!.ModuleId,
                VocabularyItemId = item.VocabularyItemId!.Value
            })
            .ToListAsync(cancellationToken);

        var practicedVocabularyCounts = practicedFromMastery
            .Concat(practicedFromItemAttempts)
            .GroupBy(item => item.ModuleId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.VocabularyItemId).Distinct().Count());

        var moduleDtos = modules.Select(module =>
        {
            challengeCounts.TryGetValue(module.Id, out var challengeCount);
            progress.TryGetValue(module.Id, out var moduleProgress);
            masteryStats.TryGetValue(module.Id, out var mastery);
            var vocabularyCount = vocabularyCounts.GetValueOrDefault(module.Id);
            var generated = challengeCount?.Generated ?? 0;
            var progressPercent = moduleProgress?.Progress ?? 0;
            var weakWordCount = mastery?.Weak ?? 0;
            return new ModuleSummaryDto(
                module.Id,
                string.IsNullOrWhiteSpace(module.UnitTitle) ? module.Title : module.UnitTitle,
                module.UnitNumber,
                module.Subject,
                module.YearLevel,
                vocabularyCount,
                practicedVocabularyCounts.GetValueOrDefault(module.Id),
                generated,
                challengeCount?.Active ?? 0,
                progressPercent,
                weakWordCount,
                generated,
                moduleProgress?.Completed ?? 0,
                mastery?.AverageScore ?? 0,
                challengeCount?.LastActivityAt,
                ResolveModuleProgressStatus(generated, progressPercent, weakWordCount));
        })
            .Where(module => module.VocabularyCount > 0)
            .ToList();

        var subjectGroups = moduleDtos
            .GroupBy(m => m.Subject)
            .Select(g => new SubjectModuleGroupDto(
                g.Key,
                g.Count(),
                g.Any() ? (int)Math.Round(g.Average(m => m.ProgressPercent)) : 0,
                g.ToList()))
            .OrderBy(g => g.Subject)
            .ToList();

        var customChallengeCounts = await GetCustomModuleSummaryAsync(customModule.Id, cancellationToken);
        var activeChallengeCount = await dbContext.Challenges.AsNoTracking()
            .CountAsync(c => c.ClassroomId == classroomId && (c.SourceType == null || c.SourceType != RecoverySourceType) && ActiveStates.Contains(c.LifecycleState), cancellationToken);

        return new ClassroomModuleOverviewDto(
            classroom.Id,
            classroom.Name,
            classroom.YearLevel,
            classroom.JoinCode,
            studentCount,
            activeChallengeCount,
            RecommendedActions(),
            subjectGroups,
            new CustomModuleSummaryDto(customModule.Id, GetModuleDisplayName(customModule), customChallengeCounts.Total, customChallengeCounts.Active));
    }

    public async Task<IReadOnlyList<SubjectModuleGroupDto>> GetClassroomModulesAsync(int classroomId, int teacherId, CancellationToken cancellationToken)
    {
        var overview = await GetModuleOverviewAsync(classroomId, teacherId, cancellationToken);
        return overview.SubjectGroups;
    }

    public async Task<CustomModuleSummaryDto> GetCustomModuleAsync(int classroomId, int teacherId, CancellationToken cancellationToken)
    {
        var classroom = await GetTeacherClassroomAsync(classroomId, teacherId, cancellationToken);
        var customModule = await EnsureCustomModuleAsync(classroom, teacherId, cancellationToken);
        var counts = await GetCustomModuleSummaryAsync(customModule.Id, cancellationToken);
        return new CustomModuleSummaryDto(customModule.Id, GetModuleDisplayName(customModule), counts.Total, counts.Active);
    }

    public async Task<IReadOnlyList<ModuleChallengeDto>> GetModuleChallengesAsync(int moduleId, int classroomId, int teacherId, CancellationToken cancellationToken)
    {
        var classroom = await GetTeacherClassroomAsync(classroomId, teacherId, cancellationToken);
        await EnsureClassroomModuleLinksAsync(classroom, teacherId, cancellationToken);
        await EnsureModuleAttachedAsync(classroomId, moduleId, cancellationToken);
        var challenges = await dbContext.Challenges.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.GameTemplate)
            .Include(c => c.AiAuditLog)
            .Include(c => c.Progresses)
            .Where(c => c.ClassroomId == classroomId && c.ModuleId == moduleId)
            .Where(c => c.SourceType == null || c.SourceType != RecoverySourceType)
            .OrderByDescending(c => c.ModifiedDate ?? c.CreatedTime)
            .ToListAsync(cancellationToken);

        return challenges.Select(ToChallengeDto).ToList();
    }

    public async Task<ModuleChallengeDto> GenerateModuleChallengeAsync(int moduleId, GenerateModuleChallengeRequest request, int teacherId, CancellationToken cancellationToken)
    {
        var classroom = await GetTeacherClassroomAsync(request.ClassroomId, teacherId, cancellationToken);
        var module = await dbContext.SyllabusModules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.IsActive && m.ModuleType == SyllabusModule.PredefinedModuleType, cancellationToken)
            ?? throw new InvalidOperationException("Syllabus module not found");

        if (module.YearLevel != classroom.YearLevel)
            throw new InvalidOperationException("Module year level does not match classroom year level");

        await EnsureClassroomModuleLinksAsync(classroom, teacherId, cancellationToken);
        await EnsureModuleAttachedAsync(classroom.Id, moduleId, cancellationToken);
        await EnsureModuleChallengeCapacityAsync(classroom.Id, module.Id, cancellationToken);

        var vocabulary = await dbContext.VocabularyItems.AsNoTracking()
            .Where(v => v.ModuleId == module.Id && v.IsActive)
            .OrderBy(v => v.DisplayOrder)
            .ThenBy(v => v.Word)
            .ToListAsync(cancellationToken);

        if (vocabulary.Count == 0)
            throw new InvalidOperationException("Module has no active vocabulary items.");

        var weakness = await GetModuleWeaknessAsync(classroom.Id, module.Id, cancellationToken);
        var moduleTitle = string.IsNullOrWhiteSpace(module.UnitTitle) ? module.Title : module.UnitTitle;
        var aiPlan = await challengeAiPipelineService.GenerateModuleChallengePlanAsync(
            new ModuleChallengePlanRequest(
                module.Id,
                moduleTitle,
                module.Subject,
                module.YearLevel,
                request.GameType,
                request.Mode,
                vocabulary.Select(ToAiItem).ToList(),
                weakness.WeakWords,
                weakness.WeakSkill,
                teacherId,
                classroom.Id),
            cancellationToken);

        if (aiPlan.IsSuccess)
        {
            var selectedItems = SelectVocabularyItems(vocabulary, aiPlan.Result.SelectedWords);
            var config = await challengeAiPipelineService.GenerateGameConfigAsync(
                new GameConfigGenerationRequest(
                    module.Id,
                    moduleTitle,
                    module.Subject,
                    classroom.Id,
                    request.Mode,
                    "PREDEFINED_MODULE",
                    aiPlan.Result.RecommendedGameType,
                    aiPlan.Result.DifficultyLevel,
                    selectedItems.Select(ToAdaptiveItem).ToList()),
                cancellationToken);

            if (config.IsSuccess)
            {
                var assigned = await challengeOrchestrator.AssignAsync(new AssignAdaptiveChallengeRequest(
                    teacherId, null, classroom.Id, null,
                    config.Result with { SourceType = "PREDEFINED_MODULE", ModuleId = module.Id },
                    module.Subject,
                    null,
                    AiGenerationStatuses.AiAssisted,
                    AiUseCases.ModuleChallengePlanning,
                    aiPlan.Result.AiAuditLogId), cancellationToken);

                if (aiPlan.Result.AiAuditLogId is int auditLogId)
                    await aiAuditService.AttachChallengeAsync(auditLogId, assigned.ChallengeId, cancellationToken);

                return await GetGeneratedChallengeDtoAsync(assigned.ChallengeId, cancellationToken);
            }

            logger.LogWarning(
                "Gemini game config generation failed for module {ModuleId}: {Error}. Falling back to rule-based module generation.",
                module.Id,
                config.ErrorMessage);
        }
        else
        {
            logger.LogWarning(
                "Gemini module plan generation failed for module {ModuleId}: {Error}. Falling back to rule-based module generation.",
                module.Id,
                aiPlan.ErrorMessage);
        }

        var failedAiAuditLogId = aiPlan.Result?.AiAuditLogId is > 0
            ? aiPlan.Result.AiAuditLogId
            : null;
        var fallback = await GenerateRuleBasedModuleChallengeAsync(
            module,
            classroom.Id,
            request,
            teacherId,
            failedAiAuditLogId,
            true,
            cancellationToken);

        return await GetGeneratedChallengeDtoAsync(fallback.ChallengeId, cancellationToken);
    }

    private async Task<AssignedAdaptiveChallengeDto> GenerateRuleBasedModuleChallengeAsync(
        SyllabusModule module,
        int classroomId,
        GenerateModuleChallengeRequest request,
        int teacherId,
        int? failedAiAuditLogId,
        bool fallbackUsed,
        CancellationToken cancellationToken)
    {
        var preview = await challengeOrchestrator.GenerateAsync(new GenerateAdaptiveChallengeRequest(
            "class", null, classroomId, request.Mode, "PREDEFINED_MODULE", module.Id,
            request.GameType, request.Mode.Replace('_', ' '), null, null, null), cancellationToken);

        var assigned = await challengeOrchestrator.AssignAsync(new AssignAdaptiveChallengeRequest(
            teacherId, null, classroomId, null,
            preview with { SourceType = "PREDEFINED_MODULE", ModuleId = module.Id },
            module.Subject,
            null,
            fallbackUsed ? AiGenerationStatuses.FailedFallback : AiGenerationStatuses.None,
            fallbackUsed ? AiUseCases.ModuleChallengePlanning : null,
            failedAiAuditLogId), cancellationToken);

        if (failedAiAuditLogId is int auditLogId)
            await aiAuditService.AttachChallengeAsync(auditLogId, assigned.ChallengeId, cancellationToken);

        return assigned;
    }

    private async Task<ModuleChallengeDto> GetGeneratedChallengeDtoAsync(int challengeId, CancellationToken cancellationToken)
    {
        var challenge = await dbContext.Challenges.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.GameTemplate)
            .Include(c => c.AiAuditLog)
            .Include(c => c.Progresses)
            .FirstOrDefaultAsync(c => c.Id == challengeId, cancellationToken)
            ?? throw new InvalidOperationException("Generated challenge not found");

        return ToChallengeDto(challenge);
    }

    public async Task<IReadOnlyList<ModuleChallengeDto>> GetCustomModuleChallengesAsync(int customModuleId, int teacherId, CancellationToken cancellationToken)
    {
        var customModule = await GetTeacherCustomModuleAsync(customModuleId, teacherId, cancellationToken);
        var challenges = await dbContext.Challenges.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.GameTemplate)
            .Include(c => c.AiAuditLog)
            .Include(c => c.Progresses)
            .Where(c => c.ModuleId == customModule.Id)
            .Where(c => c.SourceType == null || c.SourceType != RecoverySourceType)
            .OrderByDescending(c => c.ModifiedDate ?? c.CreatedTime)
            .ToListAsync(cancellationToken);

        return challenges.Select(ToChallengeDto).ToList();
    }

    public async Task<CustomModuleSummaryDto> RenameCustomModuleAsync(int customModuleId, RenameCustomModuleRequest request, int teacherId, CancellationToken cancellationToken)
    {
        var customModule = await GetTeacherCustomModuleAsync(customModuleId, teacherId, cancellationToken, tracking: true);
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Custom module name is required");

        customModule.Title = name;
        customModule.UnitTitle = name;
        await dbContext.SaveChangesAsync(cancellationToken);
        var counts = await GetCustomModuleSummaryAsync(customModule.Id, cancellationToken);
        return new CustomModuleSummaryDto(customModule.Id, GetModuleDisplayName(customModule), counts.Total, counts.Active);
    }

    public async Task<AssignedAdaptiveChallengeDto> CreateCustomModuleChallengeAsync(int customModuleId, CreateCustomModuleChallengeRequest request, int teacherId, CancellationToken cancellationToken)
    {
        var customModule = await GetTeacherCustomModuleAsync(customModuleId, teacherId, cancellationToken);
        var words = request.Items?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? new List<string>();
        if (words.Count == 0)
            throw new InvalidOperationException("At least one item is required");

        await EnsureModuleChallengeCapacityAsync(GetCustomModuleClassroomId(customModule), customModule.Id, cancellationToken);

        var preview = await challengeOrchestrator.GenerateAsync(new GenerateAdaptiveChallengeRequest(
            "class", null, GetCustomModuleClassroomId(customModule), "CUSTOM_MODULE", "manual_input", customModule.Id,
            request.GameType, "custom module", words, null, null), cancellationToken);

        var title = string.IsNullOrWhiteSpace(request.Title) ? "Custom Challenge" : request.Title.Trim();

        return await challengeOrchestrator.AssignAsync(new AssignAdaptiveChallengeRequest(
            teacherId, null, GetCustomModuleClassroomId(customModule), null,
            preview with { Title = title, SourceType = "CUSTOM_MODULE", ModuleId = customModule.Id },
            customModule.Subject, null), cancellationToken);
    }

    public async Task<bool> DeleteCustomModuleAsync(int customModuleId, int teacherId, CancellationToken cancellationToken)
    {
        var customModule = await GetTeacherCustomModuleAsync(customModuleId, teacherId, cancellationToken, tracking: true);
        var linkedChallenges = await dbContext.Challenges
            .Where(c => c.ModuleId == customModule.Id)
            .ToListAsync(cancellationToken);

        if (linkedChallenges.Count == 0 || linkedChallenges.Any(c => !IsArchived(c)))
            throw new InvalidOperationException("Only archived modules can be deleted");

        await DeleteChallengeDependentsAsync(linkedChallenges.Select(c => c.Id).ToArray(), cancellationToken);
        dbContext.Challenges.RemoveRange(linkedChallenges);
        dbContext.ClassroomModules.RemoveRange(customModule.ClassroomModules);
        dbContext.SyllabusModules.Remove(customModule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteChallengeAsync(int challengeId, int teacherId, CancellationToken cancellationToken)
    {
        var challenge = await dbContext.Challenges
            .Include(c => c.Classroom)
            .FirstOrDefaultAsync(c => c.Id == challengeId, cancellationToken)
            ?? throw new InvalidOperationException("Challenge not found");

        var isOwnedByTeacher =
            challenge.CreatedById == teacherId ||
            challenge.Classroom?.TeacherId == teacherId;

        if (!isOwnedByTeacher)
            throw new UnauthorizedAccessException("You do not manage this challenge");

        if (!IsArchived(challenge))
            throw new InvalidOperationException($"Only archived challenges can be deleted. Current state is {challenge.LifecycleState} / {challenge.Status}.");

        await DeleteChallengeDependentsAsync(new[] { challenge.Id }, cancellationToken);
        dbContext.Challenges.Remove(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Classroom> GetTeacherClassroomAsync(int classroomId, int teacherId, CancellationToken cancellationToken, bool isAdmin = false)
    {
        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(c => c.Id == classroomId && c.IsActive && !c.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found");
        if (!isAdmin && classroom.TeacherId != teacherId)
            throw new UnauthorizedAccessException("You do not manage this classroom");
        return classroom;
    }

    private static bool IsArchived(Challenge challenge) =>
        challenge.LifecycleState == ChallengeLifecycleState.Archived ||
        string.Equals(challenge.Status, "archived", StringComparison.OrdinalIgnoreCase);

    private async Task DeleteChallengeDependentsAsync(IReadOnlyCollection<int> challengeIds, CancellationToken cancellationToken)
    {
        if (challengeIds.Count == 0)
            return;

        await dbContext.AiAuditLogs
            .Where(log => log.RelatedChallengeId.HasValue && challengeIds.Contains(log.RelatedChallengeId.Value))
            .ExecuteUpdateAsync(setters => setters.SetProperty(log => log.RelatedChallengeId, (int?)null), cancellationToken);

        await dbContext.RecoveryMissions
            .Where(mission => mission.LinkedChallengeId.HasValue && challengeIds.Contains(mission.LinkedChallengeId.Value))
            .ExecuteUpdateAsync(setters => setters.SetProperty(mission => mission.LinkedChallengeId, (int?)null), cancellationToken);

        var challengeItemIds = await dbContext.ChallengeItems
            .Where(item => challengeIds.Contains(item.ChallengeId))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        var studentAttemptIds = await dbContext.StudentChallengeAttempts
            .Where(attempt => challengeIds.Contains(attempt.ChallengeId))
            .Select(attempt => attempt.Id)
            .ToListAsync(cancellationToken);

        var itemAttemptIds = await dbContext.StudentChallengeItemAttempts
            .Where(attempt =>
                studentAttemptIds.Contains(attempt.StudentChallengeAttemptId) ||
                challengeItemIds.Contains(attempt.ChallengeItemId))
            .Select(attempt => attempt.Id)
            .ToListAsync(cancellationToken);

        if (itemAttemptIds.Count > 0)
        {
            await dbContext.ErrorPatternLogs
                .Where(log => log.ChallengeItemAttemptId.HasValue && itemAttemptIds.Contains(log.ChallengeItemAttemptId.Value))
                .ExecuteUpdateAsync(setters => setters.SetProperty(log => log.ChallengeItemAttemptId, (int?)null), cancellationToken);

            await dbContext.StudentChallengeItemAttempts
                .Where(attempt => itemAttemptIds.Contains(attempt.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (studentAttemptIds.Count > 0)
        {
            await dbContext.StudentChallengeAttempts
                .Where(attempt => studentAttemptIds.Contains(attempt.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await dbContext.Attempts
            .Where(attempt => challengeIds.Contains(attempt.ChallengeId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.ChallengeProgresses
            .Where(progress => challengeIds.Contains(progress.ChallengeId))
            .ExecuteDeleteAsync(cancellationToken);

        if (challengeItemIds.Count > 0)
        {
            await dbContext.ChallengeItems
                .Where(item => challengeItemIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    private async Task EnsureModuleChallengeCapacityAsync(int classroomId, int moduleId, CancellationToken cancellationToken)
    {
        var count = await dbContext.Challenges.AsNoTracking()
            .Where(c => c.ClassroomId == classroomId
                        && c.ModuleId == moduleId
                        && (c.SourceType == null || c.SourceType != RecoverySourceType)
                        && c.LifecycleState != ChallengeLifecycleState.Archived
                        && c.Status != "archived")
            .CountAsync(cancellationToken);

        if (count >= MaxGameChallengesPerModule)
            throw new InvalidOperationException("Each module can have up to 3 game challenges");
    }

    private async Task<SyllabusModule> GetTeacherCustomModuleAsync(int customModuleId, int teacherId, CancellationToken cancellationToken, bool tracking = false)
    {
        var query = tracking ? dbContext.SyllabusModules : dbContext.SyllabusModules.AsNoTracking();
        var customModule = await query
            .Include(module => module.ClassroomModules)
                .ThenInclude(link => link.Classroom)
            .FirstOrDefaultAsync(module => module.Id == customModuleId && module.ModuleType == SyllabusModule.CustomModuleType, cancellationToken)
            ?? throw new InvalidOperationException("Custom module not found");
        var classroom = customModule.ClassroomModules.Select(link => link.Classroom).FirstOrDefault()
            ?? throw new InvalidOperationException("Classroom not found");
        if (!classroom.IsActive || classroom.IsDeleted)
            throw new InvalidOperationException("Classroom not found");
        if (classroom.TeacherId != teacherId)
            throw new UnauthorizedAccessException("You do not manage this custom module");
        return customModule;
    }

    private async Task<SyllabusModule> EnsureCustomModuleAsync(Classroom classroom, int teacherId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ClassroomModules
            .Include(link => link.Module)
            .Where(link => link.ClassroomId == classroom.Id && link.Module.ModuleType == SyllabusModule.CustomModuleType)
            .Select(link => link.Module)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null) return existing;

        var customModule = new SyllabusModule
        {
            ModuleCode = $"CUSTOM-{classroom.Id}-{Guid.NewGuid():N}",
            Subject = string.IsNullOrWhiteSpace(classroom.Subject) ? "Custom" : classroom.Subject.Trim(),
            Language = "ms",
            YearLevel = classroom.YearLevel,
            Term = string.Empty,
            UnitTitle = "Custom Module",
            Title = "Custom Module",
            Description = "Teacher-created learning module.",
            ModuleType = SyllabusModule.CustomModuleType,
            SourceType = "teacher_created",
            CreatedByTeacherId = teacherId
        };
        dbContext.SyllabusModules.Add(customModule);
        dbContext.ClassroomModules.Add(new ClassroomModule
        {
            ClassroomId = classroom.Id,
            Module = customModule
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return customModule;
    }

    private async Task EnsureClassroomModuleLinksAsync(Classroom classroom, int teacherId, CancellationToken cancellationToken)
    {
        var subjects = await dbContext.ClassroomSubjects.AsNoTracking()
            .Where(subject => subject.ClassroomId == classroom.Id)
            .Select(subject => subject.Subject)
            .ToListAsync(cancellationToken);

        if (subjects.Count == 0 && !string.IsNullOrWhiteSpace(classroom.Subject))
        {
            subjects.Add(classroom.Subject.Trim());
        }

        subjects = subjects
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (subjects.Count == 0)
        {
            return;
        }

        var existingSubjectSet = await dbContext.ClassroomSubjects.AsNoTracking()
            .Where(subject => subject.ClassroomId == classroom.Id)
            .Select(subject => subject.Subject)
            .ToListAsync(cancellationToken);
        var existingSubjects = existingSubjectSet.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var subject in subjects.Where(subject => !existingSubjects.Contains(subject)))
        {
            dbContext.ClassroomSubjects.Add(new ClassroomSubject
            {
                ClassroomId = classroom.Id,
                Subject = subject
            });
        }

        var matchingModuleIds = await dbContext.SyllabusModules.AsNoTracking()
            .Where(module => module.IsActive
                             && module.ModuleType == SyllabusModule.PredefinedModuleType
                             && module.YearLevel == classroom.YearLevel
                             && subjects.Contains(module.Subject)
                             && dbContext.VocabularyItems.Any(vocabulary => vocabulary.ModuleId == module.Id && vocabulary.IsActive))
            .Select(module => module.Id)
            .ToListAsync(cancellationToken);

        if (matchingModuleIds.Count == 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var existingModuleIds = await dbContext.ClassroomModules.AsNoTracking()
            .Where(link => link.ClassroomId == classroom.Id && matchingModuleIds.Contains(link.ModuleId))
            .Select(link => link.ModuleId)
            .ToListAsync(cancellationToken);
        var existingModules = existingModuleIds.ToHashSet();

        foreach (var moduleId in matchingModuleIds.Where(moduleId => !existingModules.Contains(moduleId)))
        {
            dbContext.ClassroomModules.Add(new ClassroomModule
            {
                ClassroomId = classroom.Id,
                ModuleId = moduleId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureModuleAttachedAsync(int classroomId, int moduleId, CancellationToken cancellationToken)
    {
        var isAttached = await dbContext.ClassroomModules.AsNoTracking()
            .AnyAsync(link => link.ClassroomId == classroomId && link.ModuleId == moduleId, cancellationToken);
        if (!isAttached)
        {
            throw new InvalidOperationException("Module is not attached to this classroom");
        }
    }

    private async Task<(int Total, int Active)> GetCustomModuleSummaryAsync(int customModuleId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Challenges.AsNoTracking()
            .Where(c => c.ModuleId == customModuleId && (c.SourceType == null || c.SourceType != RecoverySourceType))
            .Select(c => c.LifecycleState)
            .ToListAsync(cancellationToken);
        return (rows.Count, rows.Count(ActiveStates.Contains));
    }

    private async Task<ModuleWeaknessContext> GetModuleWeaknessAsync(int classroomId, int moduleId, CancellationToken cancellationToken)
    {
        var studentIds = await dbContext.ClassroomStudents.AsNoTracking()
            .Where(s => s.ClassroomId == classroomId)
            .Select(s => s.UserId)
            .ToListAsync(cancellationToken);

        if (studentIds.Count == 0)
            return new ModuleWeaknessContext(Array.Empty<string>(), null);

        var weakRows = await dbContext.StudentWordMasteries.AsNoTracking()
            .Include(m => m.VocabularyItem)
            .Where(m => studentIds.Contains(m.StudentId)
                        && m.ModuleId == moduleId
                        && m.MasteryScore < 65)
            .OrderBy(m => m.MasteryScore)
            .ThenBy(m => m.VocabularyItem.Word)
            .Take(12)
            .ToListAsync(cancellationToken);

        var weakWords = weakRows
            .Select(m => m.VocabularyItem.Word)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var weakSkill = weakRows
            .SelectMany(m => (m.WeaknessTagsJson ?? string.Empty)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .Select(group => MapWeakSkill(group.Key))
            .FirstOrDefault(skill => skill is not null);

        return new ModuleWeaknessContext(weakWords, weakSkill);
    }

    private static ModuleChallengeAiItem ToAiItem(VocabularyItem item) =>
        new(
            item.Id,
            item.Word,
            item.BmText,
            item.EnText,
            item.ZhText,
            item.SyllablesJson,
            item.SyllableText,
            item.ItemType,
            item.DifficultyLevel,
            item.MeaningText,
            item.ExampleSentence);

    private static AdaptiveChallengeItemDto ToAdaptiveItem(VocabularyItem item) =>
        new(
            null,
            item.Id,
            item.Word,
            item.NormalizedWord,
            item.PhoneticHint ?? item.MeaningText,
            item.MeaningText,
            item.ExampleSentence,
            item.SyllablesJson,
            item.DifficultyLevel,
            item.BmText,
            item.ZhText,
            item.EnText,
            item.SyllableText,
            item.ItemType,
            item.DisplayOrder,
            null,
            null,
            null,
            null);

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

    private static bool MatchesVocabularyWord(VocabularyItem item, string selectedWord)
        => string.Equals(item.Word, selectedWord, StringComparison.OrdinalIgnoreCase)
           || string.Equals(item.BmText, selectedWord, StringComparison.OrdinalIgnoreCase)
           || string.Equals(item.EnText, selectedWord, StringComparison.OrdinalIgnoreCase)
           || string.Equals(item.ZhText, selectedWord, StringComparison.OrdinalIgnoreCase);

    private static string? MapWeakSkill(string tag)
    {
        if (tag.Contains("syllable", StringComparison.OrdinalIgnoreCase))
            return "SYLLABLE";
        if (tag.Contains("speak", StringComparison.OrdinalIgnoreCase) || tag.Contains("pronunciation", StringComparison.OrdinalIgnoreCase))
            return "SPEAKING";
        if (tag.Contains("spell", StringComparison.OrdinalIgnoreCase))
            return "SPELLING";
        return null;
    }

    private static IReadOnlyList<RecommendedActionDto> RecommendedActions() => new[]
    {
        new RecommendedActionDto("MANAGE_MODULES", "Manage syllabus modules", "Browse and configure classroom modules."),
        new RecommendedActionDto("CREATE_SPELLING_TEST", "Create Spelling Test", "Assess spelling across selected modules."),
        new RecommendedActionDto("GENERATE_RECOVERY", "Generate Recovery Challenge", "Create practice from students’ weak words.")
    };

    private static string GetModuleDisplayName(SyllabusModule module) =>
        string.IsNullOrWhiteSpace(module.UnitTitle) ? module.Title : module.UnitTitle;

    private static int GetCustomModuleClassroomId(SyllabusModule module) =>
        module.ClassroomModules.Select(link => link.ClassroomId).FirstOrDefault();

    private static ModuleChallengeDto ToChallengeDto(Challenge challenge)
    {
        var total = challenge.Progresses?.Count ?? 0;
        var completed = challenge.Progresses?.Count(p => p.HasCompleted) ?? 0;
        var progress = total > 0 ? (int)Math.Round((double)completed / total * 100) : 0;
        var lastUpdated = challenge.LastActivityAt ?? challenge.ModifiedDate ?? challenge.CreatedTime;
        var lifecycleState = ResolveModuleLifecycleState(challenge);
        var aiPlan = ReadAiPlan(challenge.AiAuditLog?.ParsedOutputJson);
        var validationErrors = ReadStringArray(challenge.AiAuditLog?.ValidationErrorsJson);
        var wasFallbackUsed = string.Equals(challenge.AiGenerationStatus, AiGenerationStatuses.FailedFallback, StringComparison.OrdinalIgnoreCase);
        var validationStatus = challenge.AiAuditLog?.ValidationStatus;
        var trustIndicators = BuildTrustIndicators(challenge, aiPlan, validationStatus, validationErrors, wasFallbackUsed);
        return new ModuleChallengeDto(
            challenge.Id,
            challenge.Title,
            challenge.Game?.Key ?? string.Empty,
            challenge.GameTemplate?.Code ?? challenge.Game?.Key ?? string.Empty,
            lifecycleState.ToString().ToUpperInvariant(),
            challenge.Status,
            progress,
            lastUpdated,
            IsArchived(challenge),
            aiPlan.SelectedWords,
            aiPlan.RecommendedGameType,
            aiPlan.DifficultyLevel,
            aiPlan.Reason,
            aiPlan.FocusType,
            validationStatus,
            validationErrors,
            challenge.AiAuditLog?.Provider,
            wasFallbackUsed,
            validationStatus,
            trustIndicators,
            ResolveGenerationSource(challenge, wasFallbackUsed));
    }

    private static ChallengeLifecycleState ResolveModuleLifecycleState(Challenge challenge)
    {
        if (string.Equals(challenge.Status, "archived", StringComparison.OrdinalIgnoreCase))
            return ChallengeLifecycleState.Archived;

        if (string.Equals(challenge.Status, "completed", StringComparison.OrdinalIgnoreCase))
            return ChallengeLifecycleState.Completed;

        return challenge.LifecycleState;
    }

    private static string ResolveModuleProgressStatus(int challengeCount, int progressPercent, int weakWordCount)
    {
        if (challengeCount == 0)
            return "NOT_STARTED";
        if (progressPercent >= 100)
            return weakWordCount > 0 ? "REVIEW_NEEDED" : "COMPLETED";
        if (progressPercent > 0)
            return "IN_PROGRESS";
        return "ASSIGNED";
    }

    private sealed record ModuleWeaknessContext(IReadOnlyList<string> WeakWords, string? WeakSkill);

    private static ModuleChallengePlanDisplay ReadAiPlan(string? parsedOutputJson)
    {
        if (string.IsNullOrWhiteSpace(parsedOutputJson))
            return ModuleChallengePlanDisplay.Empty;

        try
        {
            using var doc = JsonDocument.Parse(parsedOutputJson);
            var root = doc.RootElement;
            return new ModuleChallengePlanDisplay(
                ReadStringArray(root, "selectedWords"),
                ReadString(root, "recommendedGameType"),
                ReadInt(root, "difficultyLevel"),
                ReadString(root, "reason"),
                ReadString(root, "focusType"));
        }
        catch
        {
            return ModuleChallengePlanDisplay.Empty;
        }
    }

    private static IReadOnlyList<string> ReadStringArray(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return Array.Empty<string>();

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray()
                    .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item!)
                    .ToList()
                : Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        return value.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToList();
    }

    private static string? ReadString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsed)
            ? parsed
            : null;

    private static IReadOnlyList<string> BuildTrustIndicators(
        Challenge challenge,
        ModuleChallengePlanDisplay aiPlan,
        string? validationStatus,
        IReadOnlyList<string> validationErrors,
        bool wasFallbackUsed)
    {
        var indicators = new List<string>();
        if (wasFallbackUsed)
        {
            indicators.Add("Professor Vega used a safe rule-based plan because AI was unavailable.");
            return indicators;
        }

        if (string.Equals(validationStatus, AiValidationStatuses.Valid, StringComparison.OrdinalIgnoreCase))
        {
            if (aiPlan.SelectedWords.Count > 0)
                indicators.Add("Words verified from this module");
            if (validationErrors.Count == 0)
                indicators.Add("No outside words detected");
        }

        if (string.Equals(challenge.ChallengeMode, "WEAKNESS_REMEDIATION", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(aiPlan.FocusType, "WEAKNESS_REMEDIATION", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(aiPlan.FocusType, "FIX_WEAK_WORDS", StringComparison.OrdinalIgnoreCase))
        {
            indicators.Add("Generated using classroom weakness data");
        }

        return indicators.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ResolveGenerationSource(Challenge challenge, bool wasFallbackUsed)
    {
        if (wasFallbackUsed)
            return "Rule-based fallback";

        var provider = challenge.AiAuditLog?.Provider;
        if (!string.IsNullOrWhiteSpace(provider))
            return provider.Contains("gemini", StringComparison.OrdinalIgnoreCase) ||
                provider.Contains("google", StringComparison.OrdinalIgnoreCase)
                ? "Gemini"
                : provider;

        return challenge.IsAIGenerated ? "Gemini" : "Rule-based";
    }

    private sealed record ModuleChallengePlanDisplay(
        IReadOnlyList<string> SelectedWords,
        string? RecommendedGameType,
        int? DifficultyLevel,
        string? Reason,
        string? FocusType)
    {
        public static ModuleChallengePlanDisplay Empty { get; } = new(
            Array.Empty<string>(),
            null,
            null,
            null,
            null);
    }
}
