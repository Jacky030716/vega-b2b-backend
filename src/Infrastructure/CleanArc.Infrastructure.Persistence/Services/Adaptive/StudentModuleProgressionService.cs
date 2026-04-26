using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

public class StudentModuleProgressionService(ApplicationDbContext dbContext) : IStudentModuleProgressionService
{
    private static readonly HashSet<ChallengeLifecycleState> StudentVisibleStates = new()
    {
        ChallengeLifecycleState.Active,
        ChallengeLifecycleState.Scheduled,
        ChallengeLifecycleState.Completed
    };

    public async Task<IReadOnlyList<StudentModuleTrackDto>> GetClassroomModulesAsync(
        int classroomId,
        int studentId,
        CancellationToken cancellationToken)
    {
        var classroom = await GetStudentClassroomAsync(classroomId, studentId, cancellationToken);

        var modules = await dbContext.SyllabusModules.AsNoTracking()
            .Where(module => module.IsActive && module.YearLevel == classroom.YearLevel)
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
                challenge.CustomModuleId == null &&
                StudentVisibleStates.Contains(challenge.LifecycleState))
            .ToListAsync(cancellationToken);

        var challengeGroups = challengeRows
            .GroupBy(challenge => challenge.ModuleId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        return modules.Select(module =>
        {
            challengeGroups.TryGetValue(module.Id, out var challenges);
            challenges ??= new List<Challenge>();

            var completed = challenges.Count(challenge =>
                challenge.LifecycleState == ChallengeLifecycleState.Completed ||
                challenge.Progresses.Any(progress => progress.HasCompleted));
            var active = challenges.Count(challenge =>
                challenge.LifecycleState is ChallengeLifecycleState.Active or ChallengeLifecycleState.Scheduled);
            var progress = challenges.Count > 0
                ? (int)Math.Round((double)completed / challenges.Count * 100)
                : 0;

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
                challenges.Any(challenge => challenge.IsPinned || challenge.RecommendedScore > 0));
        }).ToList();
    }

    public async Task<StudentModuleProgressionDto> GetModuleProgressionAsync(
        int moduleId,
        int classroomId,
        int studentId,
        CancellationToken cancellationToken)
    {
        var classroom = await GetStudentClassroomAsync(classroomId, studentId, cancellationToken);
        var module = await dbContext.SyllabusModules.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == moduleId && item.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Module not found");

        if (module.YearLevel != classroom.YearLevel)
            throw new InvalidOperationException("Module year level does not match classroom year level");

        var challenges = await dbContext.Challenges.AsNoTracking()
            .Include(challenge => challenge.Game)
            .Include(challenge => challenge.GameTemplate)
            .Include(challenge => challenge.Progresses.Where(progress =>
                progress.UserId == studentId && progress.ClassroomId == classroomId))
            .Where(challenge =>
                challenge.ClassroomId == classroomId &&
                challenge.ModuleId == moduleId &&
                challenge.CustomModuleId == null &&
                StudentVisibleStates.Contains(challenge.LifecycleState))
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
            .Include(challenge => challenge.Progresses.Where(progress =>
                progress.UserId == studentId && progress.ClassroomId == classroomId))
            .Where(challenge =>
                challenge.ClassroomId == classroomId &&
                challenge.CustomModuleId != null &&
                challenge.ModuleId == null &&
                StudentVisibleStates.Contains(challenge.LifecycleState))
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
            .FirstOrDefaultAsync(item => item.Id == classroomId && item.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found");

        var isMember = await dbContext.ClassroomStudents.AsNoTracking()
            .AnyAsync(item => item.ClassroomId == classroomId && item.UserId == studentId, cancellationToken);
        if (!isMember)
            throw new UnauthorizedAccessException("You do not belong to this classroom");

        return classroom;
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
}
