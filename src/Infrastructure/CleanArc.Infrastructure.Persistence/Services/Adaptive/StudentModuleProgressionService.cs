using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

public class StudentModuleProgressionService(ApplicationDbContext dbContext) : IStudentModuleProgressionService
{
    private const string RecoverySourceType = "RECOVERY_MISSION";
    private static readonly HashSet<ChallengeLifecycleState> StudentVisibleStates = new()
    {
        ChallengeLifecycleState.Active,
        ChallengeLifecycleState.Scheduled,
        ChallengeLifecycleState.Completed
    };
    private static readonly HashSet<string> StudentVisibleStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "assigned",
        "active",
        "scheduled",
        "completed"
    };

    public async Task<IReadOnlyList<StudentModuleTrackDto>> GetClassroomModulesAsync(
        int classroomId,
        int studentId,
        CancellationToken cancellationToken)
        => await GetClassroomModulesInternalAsync(classroomId, studentId, null, cancellationToken);

    public async Task<IReadOnlyList<StudentModuleTrackDto>> GetClassroomModulesAsync(
        int classroomId,
        int studentId,
        string subject,
        CancellationToken cancellationToken)
        => await GetClassroomModulesInternalAsync(classroomId, studentId, subject, cancellationToken);

    private async Task<IReadOnlyList<StudentModuleTrackDto>> GetClassroomModulesInternalAsync(
        int classroomId,
        int studentId,
        string? subject,
        CancellationToken cancellationToken)
    {
        var classroom = await GetStudentClassroomAsync(classroomId, studentId, cancellationToken);
        await EnsureClassroomModuleLinksAsync(classroom, cancellationToken);

        var moduleQuery = dbContext.ClassroomModules.AsNoTracking()
            .Include(link => link.Module)
            .Where(link => link.ClassroomId == classroomId
                           && link.Module.IsActive
                           && link.Module.ModuleType == SyllabusModule.PredefinedModuleType);

        if (!string.IsNullOrWhiteSpace(subject))
        {
            var normalizedSubject = subject.Trim();
            moduleQuery = moduleQuery.Where(link => link.Module.Subject == normalizedSubject);
        }

        var modules = await moduleQuery
            .Select(link => link.Module)
            .OrderBy(module => module.Subject)
            .ThenBy(module => module.UnitNumber ?? int.MaxValue)
            .ThenBy(module => module.Title)
            .ToListAsync(cancellationToken);

        var moduleIds = modules.Select(module => module.Id).ToArray();
        var vocabularyCounts = await dbContext.VocabularyItems.AsNoTracking()
            .Where(item => moduleIds.Contains(item.ModuleId) && item.IsActive)
            .GroupBy(item => item.ModuleId)
            .Select(group => new { ModuleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ModuleId, item => item.Count, cancellationToken);

        var challengeRows = await dbContext.Challenges.AsNoTracking()
            .Include(challenge => challenge.Progresses.Where(progress =>
                progress.UserId == studentId && progress.ClassroomId == classroomId))
            .Where(challenge =>
                challenge.ClassroomId == classroomId &&
                challenge.ModuleId != null &&
                moduleIds.Contains(challenge.ModuleId.Value) &&
                (challenge.SourceType == null || challenge.SourceType != RecoverySourceType) &&
                (StudentVisibleStates.Contains(challenge.LifecycleState) ||
                 StudentVisibleStatuses.Contains(challenge.Status)))
            .ToListAsync(cancellationToken);

        var challengeGroups = challengeRows
            .GroupBy(challenge => challenge.ModuleId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var progresses = await dbContext.WordProgresses.AsNoTracking()
            .Include(wp => wp.Word)
            .Where(wp => wp.StudentId == studentId && moduleIds.Contains(wp.Word.ModuleId))
            .ToListAsync(cancellationToken);

        var masteryStats = progresses
            .Select(wp => new
            {
                ModuleId = wp.Word.ModuleId,
                DecayedScore = MasteryEngine.GetDecayedMasteryScore(wp.MasteryScore, wp.LastPracticedAt)
            })
            .GroupBy(x => x.ModuleId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    ModuleId = group.Key,
                    Weak = group.Count(x => x.DecayedScore < 50),
                    AverageScore = group.Any() ? Math.Round(group.Average(x => (decimal)x.DecayedScore), 2) : 0m
                });

        return modules.Select(module =>
        {
            challengeGroups.TryGetValue(module.Id, out var challenges);
            challenges ??= new List<Challenge>();
            masteryStats.TryGetValue(module.Id, out var mastery);

            var completed = challenges.Count(challenge =>
                challenge.LifecycleState == ChallengeLifecycleState.Completed ||
                challenge.Progresses.Any(progress => progress.HasCompleted));
            // Exclude player-completed challenges from the active bucket so total is not double-counted
            var active = challenges.Count(challenge =>
                challenge.LifecycleState is ChallengeLifecycleState.Active or ChallengeLifecycleState.Scheduled
                && challenge.Progresses.All(progress => !progress.HasCompleted));
            var progress = challenges.Count > 0
                ? (int)Math.Round((double)completed / challenges.Count * 100)
                : 0;
            var weakWordCount = mastery?.Weak ?? 0;
            var lastActivityAt = challenges.Count == 0
                ? (DateTime?)null
                : challenges.Max(challenge => challenge.LastActivityAt ?? challenge.ModifiedDate ?? challenge.CreatedTime);

            return new StudentModuleTrackDto(
                module.Id,
                string.IsNullOrWhiteSpace(module.UnitTitle) ? module.Title : module.UnitTitle,
                module.Subject,
                module.UnitNumber,
                module.YearLevel,
                vocabularyCounts.GetValueOrDefault(module.Id),
                active,
                completed,
                progress,
                challenges.Any(challenge => challenge.IsPinned || challenge.RecommendedScore > 0),
                challenges.Count,
                weakWordCount,
                mastery?.AverageScore ?? 0,
                lastActivityAt,
                ResolveModuleProgressStatus(challenges.Count, progress, weakWordCount));
        }).ToList();
    }

    public async Task<IReadOnlyList<string>> GetClassroomSubjectsAsync(
        int classroomId,
        int studentId,
        CancellationToken cancellationToken)
    {
        var classroom = await GetStudentClassroomAsync(classroomId, studentId, cancellationToken);
        await EnsureClassroomModuleLinksAsync(classroom, cancellationToken);
        var subjects = await dbContext.ClassroomModules.AsNoTracking()
            .Include(link => link.Module)
            .Where(link => link.ClassroomId == classroomId && link.Module.IsActive)
            .Where(link => link.Module.ModuleType == SyllabusModule.PredefinedModuleType)
            .Select(link => link.Module.Subject)
            .Distinct()
            .OrderBy(subject => subject)
            .ToListAsync(cancellationToken);

        if (subjects.Count > 0)
        {
            return subjects;
        }

        return string.IsNullOrWhiteSpace(classroom.Subject)
            ? Array.Empty<string>()
            : new[] { classroom.Subject.Trim() };
    }

    public async Task<StudentModuleProgressionDto> GetModuleProgressionAsync(
        int moduleId,
        int classroomId,
        int studentId,
        CancellationToken cancellationToken)
    {
        var module = await dbContext.SyllabusModules.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == moduleId && item.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Module not found");

        var classroom = await GetStudentClassroomAsync(classroomId, studentId, cancellationToken);
        await EnsureClassroomModuleLinksAsync(classroom, cancellationToken);
        var isAttached = await dbContext.ClassroomModules.AsNoTracking()
            .AnyAsync(link => link.ClassroomId == classroomId && link.ModuleId == moduleId, cancellationToken);
        if (!isAttached)
            throw new InvalidOperationException("Module is not attached to this classroom");

        var challenges = await dbContext.Challenges.AsNoTracking()
            .Include(challenge => challenge.Game)
            .Include(challenge => challenge.GameTemplate)
            .Include(challenge => challenge.Progresses.Where(progress =>
                progress.UserId == studentId && progress.ClassroomId == classroomId))
            .Where(challenge =>
                challenge.ClassroomId == classroomId &&
                challenge.ModuleId == moduleId &&
                (challenge.SourceType == null || challenge.SourceType != RecoverySourceType) &&
                (StudentVisibleStates.Contains(challenge.LifecycleState) ||
                 StudentVisibleStatuses.Contains(challenge.Status)))
            .OrderBy(challenge => challenge.OrderIndex)
            .ThenBy(challenge => challenge.DifficultyLevel)
            .ThenBy(challenge => challenge.Id)
            .ToListAsync(cancellationToken);

        return new StudentModuleProgressionDto(
            module.Id,
            string.IsNullOrWhiteSpace(module.UnitTitle) ? module.Title : module.UnitTitle,
            module.Subject,
            module.UnitNumber,
            ToProgressionNodes(challenges));
    }

    public async Task<IReadOnlyList<StudentCustomChallengeDto>> GetCustomChallengesAsync(
        int classroomId,
        int studentId,
        CancellationToken cancellationToken)
    {
        await GetStudentClassroomAsync(classroomId, studentId, cancellationToken);

        var challenges = await dbContext.Challenges.AsNoTracking()
            .Include(challenge => challenge.Game)
            .Include(challenge => challenge.GameTemplate)
            .Include(challenge => challenge.Module)
            .Include(challenge => challenge.Progresses.Where(progress =>
                progress.UserId == studentId && progress.ClassroomId == classroomId))
            .Where(challenge =>
                challenge.ClassroomId == classroomId &&
                challenge.ModuleId != null &&
                challenge.Module != null &&
                challenge.Module.ModuleType == SyllabusModule.CustomModuleType &&
                (challenge.SourceType == null || challenge.SourceType != RecoverySourceType) &&
                (StudentVisibleStates.Contains(challenge.LifecycleState) ||
                 StudentVisibleStatuses.Contains(challenge.Status)))
            .OrderByDescending(challenge => challenge.LastActivityAt ?? challenge.ModifiedDate ?? challenge.CreatedTime)
            .ThenBy(challenge => challenge.Id)
            .ToListAsync(cancellationToken);

        return challenges.Select(challenge =>
        {
            var progress = challenge.Progresses.FirstOrDefault();
            var isCompleted = challenge.LifecycleState == ChallengeLifecycleState.Completed || progress?.HasCompleted == true;
            return new StudentCustomChallengeDto(
                challenge.Id,
                challenge.Title,
                challenge.Description,
                challenge.Game?.Key ?? ChallengeGenerator.ToGameKey(challenge.GameTemplate?.Code ?? string.Empty),
                ToNodeType(challenge),
                isCompleted ? "COMPLETED" : "AVAILABLE",
                isCompleted ? 100 : progress?.AttemptCount > 0 ? 50 : 0,
                challenge.IsPinned || challenge.RecommendedScore > 0,
                progress?.BestStars ?? 0,
                NormalizeContentData(challenge.ContentData),
                challenge.LastActivityAt ?? challenge.ModifiedDate ?? challenge.CreatedTime);
        }).ToList();
    }

    private async Task<Domain.Entities.Classroom.Classroom> GetStudentClassroomAsync(
        int classroomId,
        int studentId,
        CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == classroomId && item.IsActive && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found");

        var isMember = await dbContext.ClassroomStudents.AsNoTracking()
            .AnyAsync(item => item.ClassroomId == classroomId && item.UserId == studentId, cancellationToken);
        if (!isMember)
            throw new UnauthorizedAccessException("You do not belong to this classroom");

        return classroom;
    }

    private async Task EnsureClassroomModuleLinksAsync(
        Domain.Entities.Classroom.Classroom classroom,
        CancellationToken cancellationToken)
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

        var existingSubjectRows = await dbContext.ClassroomSubjects.AsNoTracking()
            .Where(subject => subject.ClassroomId == classroom.Id)
            .Select(subject => subject.Subject)
            .ToListAsync(cancellationToken);
        var existingSubjects = existingSubjectRows.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var subject in subjects.Where(subject => !existingSubjects.Contains(subject)))
        {
            dbContext.ClassroomSubjects.Add(new Domain.Entities.Classroom.ClassroomSubject
            {
                ClassroomId = classroom.Id,
                Subject = subject
            });
        }

        var matchingModuleIds = await dbContext.SyllabusModules.AsNoTracking()
            .Where(module => module.IsActive
                             && module.ModuleType == SyllabusModule.PredefinedModuleType
                             && module.YearLevel == classroom.YearLevel
                             && subjects.Contains(module.Subject))
            .Select(module => module.Id)
            .ToListAsync(cancellationToken);

        var existingModuleIds = await dbContext.ClassroomModules.AsNoTracking()
            .Where(link => link.ClassroomId == classroom.Id && matchingModuleIds.Contains(link.ModuleId))
            .Select(link => link.ModuleId)
            .ToListAsync(cancellationToken);
        var existingModules = existingModuleIds.ToHashSet();

        foreach (var moduleId in matchingModuleIds.Where(moduleId => !existingModules.Contains(moduleId)))
        {
            dbContext.ClassroomModules.Add(new Domain.Entities.Classroom.ClassroomModule
            {
                ClassroomId = classroom.Id,
                ModuleId = moduleId
            });
        }

        var hasCustomModule = await dbContext.ClassroomModules.AsNoTracking()
            .Include(link => link.Module)
            .AnyAsync(link => link.ClassroomId == classroom.Id && link.Module.ModuleType == SyllabusModule.CustomModuleType, cancellationToken);
        if (!hasCustomModule)
        {
            dbContext.ClassroomModules.Add(new Domain.Entities.Classroom.ClassroomModule
            {
                ClassroomId = classroom.Id,
                Module = new SyllabusModule
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
                    CreatedByTeacherId = classroom.TeacherId
                }
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<StudentProgressionNodeDto> ToProgressionNodes(IReadOnlyList<Challenge> challenges)
    {
        var nodes = new List<StudentProgressionNodeDto>(challenges.Count);
        var previousCompleted = false;
        var firstAvailableIncompleteMarked = false;

        for (var index = 0; index < challenges.Count; index++)
        {
            var challenge = challenges[index];
            var progress = challenge.Progresses.FirstOrDefault();
            var isCompleted = challenge.LifecycleState == ChallengeLifecycleState.Completed || progress?.HasCompleted == true;
            var isAvailable = index == 0 || previousCompleted;
            var status = isCompleted ? "COMPLETED" : isAvailable ? "AVAILABLE" : "LOCKED";
            var progressPercent = isCompleted ? 100 : progress?.AttemptCount > 0 ? 50 : 0;
            var isRecommended = challenge.IsPinned || challenge.RecommendedScore > 0;
            if (!isCompleted && isAvailable && !firstAvailableIncompleteMarked)
            {
                isRecommended = true;
                firstAvailableIncompleteMarked = true;
            }

            nodes.Add(new StudentProgressionNodeDto(
                challenge.Id.ToString(),
                challenge.Id,
                ToNodeType(challenge),
                challenge.Game?.Key ?? ChallengeGenerator.ToGameKey(challenge.GameTemplate?.Code ?? string.Empty),
                challenge.Title,
                challenge.Description,
                status,
                progressPercent,
                isRecommended,
                progress?.BestStars ?? 0,
                NormalizeContentData(challenge.ContentData),
                challenge.DifficultyLevel,
                challenge.OrderIndex));

            previousCompleted = isCompleted;
        }

        return nodes;
    }

    private static string ToNodeType(Challenge challenge)
    {
        var code = challenge.GameTemplate?.Code ?? challenge.Game?.Key ?? string.Empty;
        var normalized = code.Trim().ToUpperInvariant();
        return normalized switch
        {
            "SYLLABLE_SUSHI" or "SYLLABLE-SUSHI" => "SYLLABLE_SUSHI",
            "SPELL_CATCHER" or "SPELL-CATCHER" => "SPELL_CATCHER",
            "VOICE_BRIDGE" or "VOICE-BRIDGE" or "WORD_BRIDGE" or "WORD-BRIDGE" => "VOICE_BRIDGE",
            "LEARN" => "LEARN",
            _ when normalized.Contains("SUSHI") => "SYLLABLE_SUSHI",
            _ when normalized.Contains("SPELL") => "SPELL_CATCHER",
            _ when normalized.Contains("VOICE") || normalized.Contains("BRIDGE") => "VOICE_BRIDGE",
            _ => "LEARN"
        };
    }

    private static string NormalizeContentData(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return "{}";

        try
        {
            var parsed = JsonNode.Parse(rawJson);
            var normalized = NormalizeJsonNode(parsed);
            return normalized?.ToJsonString() ?? "{}";
        }
        catch
        {
            return rawJson;
        }
    }

    private static JsonNode? NormalizeJsonNode(JsonNode? node)
    {
        if (node is null) return null;

        if (node is JsonObject obj)
        {
            var normalizedObject = new JsonObject();
            foreach (var kv in obj)
            {
                normalizedObject[ToCamelCase(kv.Key)] = NormalizeJsonNode(kv.Value);
            }

            return normalizedObject;
        }

        if (node is JsonArray arr)
        {
            var normalizedArray = new JsonArray();
            foreach (var item in arr)
            {
                normalizedArray.Add(NormalizeJsonNode(item));
            }

            return normalizedArray;
        }

        return node.DeepClone();
    }

    private static string ToCamelCase(string key)
    {
        if (string.IsNullOrEmpty(key) || !char.IsUpper(key[0]))
            return key;

        return key.Length == 1
            ? key.ToLowerInvariant()
            : char.ToLowerInvariant(key[0]) + key[1..];
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
}
