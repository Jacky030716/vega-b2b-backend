using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

using CleanArc.Infrastructure.Persistence.Services.Adaptive.Strategies;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;
public class ChallengeOrchestrator(
    ApplicationDbContext dbContext,
    IChallengeGenerator challengeGenerator,
    IRecommendationEngine recommendationEngine,
    IEnumerable<IGameStrategy> strategies) : IChallengeOrchestrator
{
    public Task<IReadOnlyList<AdaptiveRecommendationDto>> RecommendForStudentAsync(
        int studentId,
        GenerateAdaptiveChallengeRequest? request,
        CancellationToken cancellationToken)
        => recommendationEngine.RecommendForStudentAsync(studentId, request, cancellationToken);

    public Task<GeneratedAdaptiveChallengePreviewDto> GenerateAsync(
        GenerateAdaptiveChallengeRequest request,
        CancellationToken cancellationToken)
        => challengeGenerator.GenerateAsync(request, cancellationToken);

    public async Task<AssignedAdaptiveChallengeDto> AssignAsync(AssignAdaptiveChallengeRequest request, CancellationToken cancellationToken)
    {
        var preview = request.Preview;
        var template = await EnsureGameTemplateAsync(preview.GameTemplateCode, preview.Category, cancellationToken);
        var game = await EnsureGameAsync(preview.GameKey, preview.Title, preview.Description, preview.Category, cancellationToken);

        var nextOrderIndex = await dbContext.Challenges.AsNoTracking()
            .Where(c => c.GameId == game.Id)
            .Select(c => (int?)c.OrderIndex)
            .MaxAsync(cancellationToken) ?? 0;

        var classroomId = request.ClassId ?? preview.ClassId;
        var moduleId = preview.ModuleId;
        if (classroomId.HasValue)
        {
            moduleId = await ResolveChallengeModuleIdAsync(classroomId.Value, moduleId, cancellationToken);
        }

        var assignedAt = DateTime.UtcNow;
        var challenge = new Challenge
        {
            GameId = game.Id,
            Title = preview.Title,
            Description = preview.Description,
            DifficultyLevel = preview.DifficultyLevel,
            ContentData = string.IsNullOrWhiteSpace(preview.ContentData) ? "{}" : preview.ContentData,
            OrderIndex = nextOrderIndex + 1,
            MaxStars = 3,
            CreatedById = request.CreatedByTeacherId,
            IsAIGenerated = request.AiGenerationStatus is AiGenerationStatuses.AiAssisted or AiGenerationStatuses.AiGenerated
                || preview.SourceType.Equals("ai_prompt", StringComparison.OrdinalIgnoreCase),
            AiGenerationStatus = request.AiGenerationStatus ?? (preview.SourceType.Equals("ai_prompt", StringComparison.OrdinalIgnoreCase)
                ? AiGenerationStatuses.AiGenerated
                : AiGenerationStatuses.None),
            AiUseCase = request.AiUseCase,
            AiAuditLogId = request.AiAuditLogId,
            ClassroomId = classroomId,
            StudentId = request.StudentId ?? preview.StudentId,
            ModuleId = moduleId,
            GameTemplateId = template.Id,
            ChallengeMode = preview.ChallengeMode,
            SourceType = preview.SourceType,
            Subject = request.Subject,
            ConfigJson = preview.ConfigJson,
            Status = "assigned",
            AssignedAt = assignedAt,
            DueAt = request.DueAt,
            LifecycleState = ChallengeLifecycleState.Active,
            LastActivityAt = assignedAt
        };

        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);

        var items = preview.Items.Select((item, index) => new ChallengeItem
        {
            ChallengeId = challenge.Id,
            VocabularyItemId = item.VocabularyItemId,
            SequenceNo = index + 1,
            SettingsJson = JsonSerializer.Serialize(new
            {
                item.Word,
                item.Hint,
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
                item.DistractorsJson,
                item.Language
            }, ChallengeGenerator.JsonOptions)
        }).ToList();

        dbContext.ChallengeItems.AddRange(items);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AssignedAdaptiveChallengeDto(
            challenge.Id,
            challenge.Title,
            template.Code,
            game.Key,
            items.Count,
            challenge.StudentId,
            challenge.ClassroomId);
    }

    private async Task<int?> ResolveChallengeModuleIdAsync(int classroomId, int? requestedModuleId, CancellationToken cancellationToken)
    {
        if (requestedModuleId.HasValue)
        {
            var isAttached = await dbContext.ClassroomModules.AsNoTracking()
                .AnyAsync(link => link.ClassroomId == classroomId && link.ModuleId == requestedModuleId.Value, cancellationToken);
            if (isAttached)
                return requestedModuleId;

            var moduleExists = await dbContext.SyllabusModules.AsNoTracking()
                .AnyAsync(module => module.Id == requestedModuleId.Value && module.IsActive, cancellationToken);
            if (!moduleExists)
                throw new InvalidOperationException("Module not found");

            dbContext.ClassroomModules.Add(new ClassroomModule
            {
                ClassroomId = classroomId,
                ModuleId = requestedModuleId.Value
            });

            return requestedModuleId;
        }

        var modules = await dbContext.ClassroomModules.AsNoTracking()
            .Include(link => link.Module)
            .Where(link => link.ClassroomId == classroomId && link.Module.IsActive)
            .Select(link => new
            {
                link.ModuleId,
                link.Module.ModuleType
            })
            .ToListAsync(cancellationToken);

        var predefinedModules = modules
            .Where(module => module.ModuleType == SyllabusModule.PredefinedModuleType)
            .ToList();
        if (predefinedModules.Count == 1)
            return predefinedModules[0].ModuleId;

        var customModuleId = modules
            .FirstOrDefault(module => module.ModuleType == SyllabusModule.CustomModuleType)
            ?.ModuleId;
        if (customModuleId.HasValue)
            return customModuleId;

        throw new InvalidOperationException("Select a classroom module before creating this challenge");
    }

    public async Task<GeneratedAdaptiveChallengePreviewDto?> GetChallengeAsync(int challengeId, CancellationToken cancellationToken)
    {
        var challenge = await dbContext.Challenges.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.GameTemplate)
            .FirstOrDefaultAsync(c => c.Id == challengeId, cancellationToken);

        if (challenge is null)
            return null;

        var challengeItems = await dbContext.ChallengeItems.AsNoTracking()
            .Include(i => i.VocabularyItem)
                .ThenInclude(v => v!.Translations)
            .Include(i => i.VocabularyItem)
                .ThenInclude(v => v!.SyllableInfo)
            .Where(i => i.ChallengeId == challenge.Id)
            .OrderBy(i => i.SequenceNo)
            .ToListAsync(cancellationToken);

        var items = challengeItems.Select(i => new AdaptiveChallengeItemDto(
                i.Id,
                i.VocabularyItemId,
                i.VocabularyItem != null ? i.VocabularyItem.Word : ReadSetting(i.SettingsJson, "word"),
                i.VocabularyItem != null ? i.VocabularyItem.NormalizedWord : null,
                i.VocabularyItem != null ? i.VocabularyItem.PhoneticHint ?? i.VocabularyItem.MeaningText : ReadSetting(i.SettingsJson, "hint"),
                i.VocabularyItem != null ? i.VocabularyItem.MeaningText : ReadSetting(i.SettingsJson, "meaningText"),
                i.VocabularyItem != null ? i.VocabularyItem.ExampleSentence : ReadSetting(i.SettingsJson, "exampleSentence"),
                i.VocabularyItem?.SyllableInfo?.SyllablesJson ?? ReadSetting(i.SettingsJson, "syllablesJson") ?? "[]",
                i.VocabularyItem != null ? i.VocabularyItem.DifficultyLevel : 1,
                GetTranslation(i.VocabularyItem, "ms", i.SettingsJson, "bmText"),
                GetTranslation(i.VocabularyItem, "zh", i.SettingsJson, "zhText"),
                GetTranslation(i.VocabularyItem, "en", i.SettingsJson, "enText"),
                i.VocabularyItem?.SyllableInfo?.SyllableText ?? ReadSetting(i.SettingsJson, "syllableText"),
                i.VocabularyItem?.ItemType ?? ReadSetting(i.SettingsJson, "itemType"),
                i.VocabularyItem?.DisplayOrder,
                ReadSetting(i.SettingsJson, "syllablePoolJson"),
                ReadSetting(i.SettingsJson, "distractorsJson"),
                ReadSetting(i.SettingsJson, "correctOrderJson"),
                ReadSetting(i.SettingsJson, "spellCatcherSpecJson"),
                i.VocabularyItem != null ? i.VocabularyItem.Language : ReadSetting(i.SettingsJson, "language")))
            .ToList();

        var code = challenge.GameTemplate?.Code ?? challenge.Game?.Key ?? string.Empty;
        var gameKey = challenge.Game?.Key ?? ChallengeGenerator.ToGameKey(code);
        
        string contentData;
        var strategy = strategies.FirstOrDefault(s => s.GameTemplateCode == code || s.GameKey == gameKey);
        if (strategy != null)
        {
            var playableContent = strategy.GeneratePlayableContent(items, challenge.DifficultyLevel, challenge.ConfigJson);
            contentData = JsonSerializer.Serialize(playableContent, ChallengeGenerator.JsonOptions);
        }
        else
        {
            contentData = challenge.ContentData;
        }

        var syllableSpec = TryReadSyllableSushiSpec(contentData);
        var spellSpec = TryReadSpellCatcherSpec(contentData);
        return new GeneratedAdaptiveChallengePreviewDto(
            challenge.Title,
            challenge.Description,
            code,
            gameKey,
            challenge.GameTemplate?.Category ?? ChallengeGenerator.ToCategory(code),
            challenge.DifficultyLevel,
            challenge.ModuleId,
            challenge.StudentId,
            challenge.ClassroomId,
            challenge.ChallengeMode ?? "assigned",
            challenge.SourceType ?? "unknown",
            contentData,
            challenge.ConfigJson,
            items,
            syllableSpec,
            spellSpec);
    }

    private async Task<GameTemplate> EnsureGameTemplateAsync(string code, string category, CancellationToken cancellationToken)
    {
        var template = await dbContext.GameTemplates.FirstOrDefaultAsync(g => g.Code == code, cancellationToken);
        if (template is not null) return template;

        template = new GameTemplate
        {
            Code = code,
            Category = category,
            Name = code.Replace('_', ' '),
            Description = $"Adaptive template for {code}.",
            SupportsAdaptiveDifficulty = true,
            IsActive = true
        };
        dbContext.GameTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);
        return template;
    }

    private async Task<Game> EnsureGameAsync(string gameKey, string title, string description, string category, CancellationToken cancellationToken)
    {
        var game = await dbContext.Games.FirstOrDefaultAsync(g => g.Key == gameKey, cancellationToken);
        if (game is not null) return game;

        game = new Game
        {
            Key = gameKey,
            Name = title,
            Description = description,
            ImageUrl = string.Empty,
            Category = category,
            SkillsTaught = "adaptive memory"
        };
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync(cancellationToken);
        return game;
    }

    private static string ReadSetting(string settingsJson, string key)
    {
        try
        {
            var node = JsonNode.Parse(settingsJson);
            return node?[key]?.GetValue<string>() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static SyllableSushiSpecDto? TryReadSyllableSushiSpec(string contentData)
    {
        try
        {
            var node = JsonNode.Parse(contentData);
            var specNode = node?["syllableSushiSpec"];
            return specNode?.Deserialize<SyllableSushiSpecDto>(ChallengeGenerator.JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static SpellCatcherSpecDto? TryReadSpellCatcherSpec(string contentData)
    {
        try
        {
            var node = JsonNode.Parse(contentData);
            var specNode = node?["spellCatcherSpec"];
            return specNode?.Deserialize<SpellCatcherSpecDto>(ChallengeGenerator.JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetTranslation(VocabularyItem? item, string langCode, string fallbackSettingsJson, string settingKey)
    {
        if (item is null) return ReadSetting(fallbackSettingsJson, settingKey);
        var trans = item.Translations?.FirstOrDefault(t => t.LanguageCode.Equals(langCode, System.StringComparison.OrdinalIgnoreCase));
        if (trans != null) return trans.TranslationText;
        if (item.Language.Equals(langCode, System.StringComparison.OrdinalIgnoreCase)) return item.Word;
        return ReadSetting(fallbackSettingsJson, settingKey);
    }
}

