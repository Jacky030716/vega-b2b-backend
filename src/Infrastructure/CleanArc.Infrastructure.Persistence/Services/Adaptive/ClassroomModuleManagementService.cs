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
    ILogger<ClassroomModuleManagementService> logger) : IClassroomModuleManagementService
{
    private static readonly HashSet<ChallengeLifecycleState> ActiveStates = new()
    {
        ChallengeLifecycleState.Active,
        ChallengeLifecycleState.Scheduled
    };

    public async Task<ClassroomModuleOverviewDto> GetModuleOverviewAsync(int classroomId, int teacherId, CancellationToken cancellationToken)
    {
        var classroom = await GetTeacherClassroomAsync(classroomId, teacherId, cancellationToken);
        var customModule = await EnsureCustomModuleAsync(classroom, teacherId, cancellationToken);
        var studentCount = await dbContext.ClassroomStudents.CountAsync(s => s.ClassroomId == classroomId, cancellationToken);

        var modules = await dbContext.SyllabusModules.AsNoTracking()
            .Where(m => m.IsActive && m.YearLevel == classroom.YearLevel)
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
            .Where(c => c.ClassroomId == classroomId && c.ModuleId != null && moduleIds.Contains(c.ModuleId.Value))
            .GroupBy(c => c.ModuleId!.Value)
            .Select(g => new
            {
                ModuleId = g.Key,
                Generated = g.Count(),
                Active = g.Count(c => c.LifecycleState == ChallengeLifecycleState.Active || c.LifecycleState == ChallengeLifecycleState.Scheduled)
            })
            .ToDictionaryAsync(x => x.ModuleId, x => x, cancellationToken);

        var progress = await dbContext.StudentWordMasteries.AsNoTracking()
            .Where(m => m.ModuleId != null && moduleIds.Contains(m.ModuleId.Value))
            .GroupBy(m => m.ModuleId!.Value)
            .Select(g => new
            {
                ModuleId = g.Key,
                Progress = (int)Math.Round(g.Average(x => x.MasteryScore)),
                Weak = g.Count(x => x.MasteryScore < 65)
            })
            .ToDictionaryAsync(x => x.ModuleId, x => x, cancellationToken);

        var moduleDtos = modules.Select(module =>
        {
            challengeCounts.TryGetValue(module.Id, out var challengeCount);
            progress.TryGetValue(module.Id, out var moduleProgress);
            return new ModuleSummaryDto(
                module.Id,
                string.IsNullOrWhiteSpace(module.UnitTitle) ? module.Title : module.UnitTitle,
                module.UnitNumber,
                module.Subject,
                module.YearLevel,
                vocabularyCounts.GetValueOrDefault(module.Id),
                challengeCount?.Generated ?? 0,
                challengeCount?.Active ?? 0,
                moduleProgress?.Progress ?? 0,
                moduleProgress?.Weak ?? 0);
        }).ToList();

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
            .CountAsync(c => c.ClassroomId == classroomId && ActiveStates.Contains(c.LifecycleState), cancellationToken);

        return new ClassroomModuleOverviewDto(
            classroom.Id,
            classroom.Name,
            classroom.YearLevel,
            classroom.JoinCode,
            studentCount,
            activeChallengeCount,
            RecommendedActions(),
            subjectGroups,
            new CustomModuleSummaryDto(customModule.Id, customModule.Name, customChallengeCounts.Total, customChallengeCounts.Active));
    }

    public async Task<IReadOnlyList<ModuleChallengeDto>> GetModuleChallengesAsync(int moduleId, int classroomId, int teacherId, CancellationToken cancellationToken)
    {
        await GetTeacherClassroomAsync(classroomId, teacherId, cancellationToken);
        var challenges = await dbContext.Challenges.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.GameTemplate)
            .Include(c => c.Progresses)
            .Where(c => c.ClassroomId == classroomId && c.ModuleId == moduleId && c.CustomModuleId == null)
            .OrderByDescending(c => c.ModifiedDate ?? c.CreatedTime)
            .ToListAsync(cancellationToken);

        return challenges.Select(ToChallengeDto).ToList();
    }

    public async Task<AssignedAdaptiveChallengeDto> GenerateModuleChallengeAsync(int moduleId, GenerateModuleChallengeRequest request, int teacherId, CancellationToken cancellationToken)
    {
        var classroom = await GetTeacherClassroomAsync(request.ClassroomId, teacherId, cancellationToken);
        var module = await dbContext.SyllabusModules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Syllabus module not found");

        if (module.YearLevel != classroom.YearLevel)
            throw new InvalidOperationException("Module year level does not match classroom year level");

        var vocabulary = await dbContext.VocabularyItems.AsNoTracking()
            .Where(v => v.ModuleId == module.Id && v.IsActive)
            .OrderBy(v => v.DisplayOrder)
            .ThenBy(v => v.Word)
            .ToListAsync(cancellationToken);

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
                weakness.WeakSkill),
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
                return await challengeOrchestrator.AssignAsync(new AssignAdaptiveChallengeRequest(
                    teacherId, null, classroom.Id, null,
                    config.Result with { SourceType = "PREDEFINED_MODULE", ModuleId = module.Id },
                    module.Subject, null), cancellationToken);
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

        return await GenerateRuleBasedModuleChallengeAsync(module, classroom.Id, request, teacherId, cancellationToken);
    }

    private async Task<AssignedAdaptiveChallengeDto> GenerateRuleBasedModuleChallengeAsync(
        SyllabusModule module,
        int classroomId,
        GenerateModuleChallengeRequest request,
        int teacherId,
        CancellationToken cancellationToken)
    {
        var preview = await challengeOrchestrator.GenerateAsync(new GenerateAdaptiveChallengeRequest(
            "class", null, classroomId, request.Mode, "PREDEFINED_MODULE", module.Id,
            request.GameType, request.Mode.Replace('_', ' '), null, null, null), cancellationToken);

        return await challengeOrchestrator.AssignAsync(new AssignAdaptiveChallengeRequest(
            teacherId, null, classroomId, null,
            preview with { SourceType = "PREDEFINED_MODULE", ModuleId = module.Id },
            module.Subject, null), cancellationToken);
    }

    public async Task<IReadOnlyList<ModuleChallengeDto>> GetCustomModuleChallengesAsync(int customModuleId, int teacherId, CancellationToken cancellationToken)
    {
        var customModule = await GetTeacherCustomModuleAsync(customModuleId, teacherId, cancellationToken);
        var challenges = await dbContext.Challenges.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.GameTemplate)
            .Include(c => c.Progresses)
            .Where(c => c.CustomModuleId == customModule.Id && c.ModuleId == null)
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

        customModule.Name = name;
        await dbContext.SaveChangesAsync(cancellationToken);
        var counts = await GetCustomModuleSummaryAsync(customModule.Id, cancellationToken);
        return new CustomModuleSummaryDto(customModule.Id, customModule.Name, counts.Total, counts.Active);
    }

    public async Task<AssignedAdaptiveChallengeDto> CreateCustomModuleChallengeAsync(int customModuleId, CreateCustomModuleChallengeRequest request, int teacherId, CancellationToken cancellationToken)
    {
        var customModule = await GetTeacherCustomModuleAsync(customModuleId, teacherId, cancellationToken);
        var words = request.Items?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? new List<string>();
        if (words.Count == 0)
            throw new InvalidOperationException("At least one item is required");

        var preview = await challengeOrchestrator.GenerateAsync(new GenerateAdaptiveChallengeRequest(
            "class", null, customModule.ClassroomId, "CUSTOM_MODULE", "manual_input", null,
            request.GameType, "custom module", words, null, null), cancellationToken);

        var title = string.IsNullOrWhiteSpace(request.Title) ? "Custom Challenge" : request.Title.Trim();

        return await challengeOrchestrator.AssignAsync(new AssignAdaptiveChallengeRequest(
            teacherId, null, customModule.ClassroomId, null,
            preview with { Title = title, SourceType = "CUSTOM_MODULE", ModuleId = null },
            null, customModule.Id), cancellationToken);
    }

    public async Task<bool> DeleteChallengeAsync(int challengeId, int teacherId, CancellationToken cancellationToken)
    {
        var challenge = await dbContext.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && c.CreatedById == teacherId, cancellationToken)
            ?? throw new InvalidOperationException("Challenge not found");

        var hasAssignments = challenge.AssignedAt.HasValue || string.Equals(challenge.Status, "assigned", StringComparison.OrdinalIgnoreCase);
        var canDelete = challenge.LifecycleState == ChallengeLifecycleState.Draft || !hasAssignments;
        if (!canDelete)
            throw new InvalidOperationException("Only draft or unassigned challenges can be deleted");

        dbContext.Challenges.Remove(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Classroom> GetTeacherClassroomAsync(int classroomId, int teacherId, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(c => c.Id == classroomId && c.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found");
        if (classroom.TeacherId != teacherId)
            throw new UnauthorizedAccessException("You do not manage this classroom");
        return classroom;
    }

    private async Task<CustomModule> GetTeacherCustomModuleAsync(int customModuleId, int teacherId, CancellationToken cancellationToken, bool tracking = false)
    {
        var query = tracking ? dbContext.CustomModules : dbContext.CustomModules.AsNoTracking();
        var customModule = await query.Include(c => c.Classroom)
            .FirstOrDefaultAsync(c => c.Id == customModuleId, cancellationToken)
            ?? throw new InvalidOperationException("Custom module not found");
        if (customModule.Classroom.TeacherId != teacherId)
            throw new UnauthorizedAccessException("You do not manage this custom module");
        return customModule;
    }

    private async Task<CustomModule> EnsureCustomModuleAsync(Classroom classroom, int teacherId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.CustomModules.FirstOrDefaultAsync(c => c.ClassroomId == classroom.Id, cancellationToken);
        if (existing is not null) return existing;

        var customModule = new CustomModule
        {
            ClassroomId = classroom.Id,
            Name = "Custom Module",
            YearLevel = classroom.YearLevel,
            CreatedByTeacherId = teacherId
        };
        dbContext.CustomModules.Add(customModule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return customModule;
    }

    private async Task<(int Total, int Active)> GetCustomModuleSummaryAsync(int customModuleId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Challenges.AsNoTracking()
            .Where(c => c.CustomModuleId == customModuleId)
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
        new RecommendedActionDto("IMPROVE_WEAK_WORDS", "Improve Weak Words", "Generate challenges from weak vocabulary."),
        new RecommendedActionDto("PRACTICE_THIS_WEEK", "Practice This Week", "Generate focused practice for a module."),
        new RecommendedActionDto("REVIEW_OVERDUE", "Review Overdue", "Generate review from overdue vocabulary.")
    };

    private static ModuleChallengeDto ToChallengeDto(Challenge challenge)
    {
        var total = challenge.Progresses?.Count ?? 0;
        var completed = challenge.Progresses?.Count(p => p.HasCompleted) ?? 0;
        var progress = total > 0 ? (int)Math.Round((double)completed / total * 100) : 0;
        var lastUpdated = challenge.LastActivityAt ?? challenge.ModifiedDate ?? challenge.CreatedTime;
        var isAssigned = challenge.AssignedAt.HasValue || string.Equals(challenge.Status, "assigned", StringComparison.OrdinalIgnoreCase);
        return new ModuleChallengeDto(
            challenge.Id,
            challenge.Title,
            challenge.Game?.Key ?? string.Empty,
            challenge.GameTemplate?.Code ?? challenge.Game?.Key ?? string.Empty,
            challenge.LifecycleState.ToString().ToUpperInvariant(),
            challenge.Status,
            progress,
            lastUpdated,
            challenge.LifecycleState == ChallengeLifecycleState.Draft || !isAssigned);
    }

    private sealed record ModuleWeaknessContext(IReadOnlyList<string> WeakWords, string? WeakSkill);
}
