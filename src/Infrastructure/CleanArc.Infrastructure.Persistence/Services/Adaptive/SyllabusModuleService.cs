using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;
public class SyllabusModuleService(ApplicationDbContext dbContext) : ISyllabusModuleService
{
    public async Task<IReadOnlyList<SyllabusModuleDto>> GetModulesAsync(
        string? subject,
        string? language,
        int? yearLevel,
        CancellationToken cancellationToken)
    {
        var query = dbContext.SyllabusModules.AsNoTracking().Where(m => m.IsActive);
        var normalizedSubject = SyllabusSubjectMapper.NormalizeSubject(subject);

        if (!string.IsNullOrWhiteSpace(normalizedSubject))
            query = query.Where(m => m.Subject.ToLower() == normalizedSubject.ToLower());

        if (!string.IsNullOrWhiteSpace(language))
            query = query.Where(m => m.Language.ToLower() == language.Trim().ToLower());

        if (yearLevel.HasValue)
            query = query.Where(m => m.YearLevel == yearLevel.Value);

        return await query
            .OrderBy(m => m.YearLevel)
            .ThenBy(m => m.UnitNumber ?? int.MaxValue)
            .ThenBy(m => m.Week)
            .ThenBy(m => m.Title)
            .Select(m => ToDto(m))
            .ToListAsync(cancellationToken);
    }

    public async Task<SyllabusModuleDto?> GetModuleAsync(int moduleId, CancellationToken cancellationToken)
    {
        var module = await dbContext.SyllabusModules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

        return module is null ? null : ToDto(module);
    }

    public async Task<IReadOnlyList<VocabularyItemDto>> GetVocabularyAsync(int moduleId, CancellationToken cancellationToken)
    {
        return await dbContext.VocabularyItems.AsNoTracking()
            .Where(v => v.ModuleId == moduleId && v.IsActive)
            .OrderBy(v => v.DisplayOrder)
            .ThenBy(v => v.Word)
            .Select(v => ToDto(v))
            .ToListAsync(cancellationToken);
    }

    public async Task<SyllabusModuleDto> CreateModuleAsync(CreateSyllabusModuleRequest request, CancellationToken cancellationToken)
    {
        var title = Require(request.Title, "Title");
        var module = new SyllabusModule
        {
            Subject = SyllabusSubjectMapper.NormalizeSubject(Require(request.Subject, "Subject")) ?? Require(request.Subject, "Subject"),
            Language = Require(request.Language, "Language"),
            YearLevel = Math.Clamp(request.YearLevel, 1, 6),
            Term = request.Term?.Trim() ?? string.Empty,
            Week = request.Week,
            ModuleCode = string.IsNullOrWhiteSpace(request.ModuleCode)
                ? $"TEACHER-{Guid.NewGuid():N}"
                : request.ModuleCode.Trim().ToUpperInvariant(),
            UnitNumber = request.UnitNumber,
            UnitTitle = request.UnitTitle?.Trim() ?? title,
            Title = title,
            Description = request.Description?.Trim() ?? string.Empty,
            SourceType = request.SourceType?.Trim() ?? "teacher_created",
            IsActive = true
        };

        dbContext.SyllabusModules.Add(module);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(module);
    }

    public async Task<VocabularyItemDto> CreateVocabularyItemAsync(
        int moduleId,
        CreateVocabularyItemRequest request,
        CancellationToken cancellationToken)
    {
        var module = await dbContext.SyllabusModules.FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken)
            ?? throw new InvalidOperationException("Syllabus module not found");

        var word = Require(request.Word, "Word");
        var bmText = string.IsNullOrWhiteSpace(request.BmText) ? word : request.BmText.Trim();
        var item = new VocabularyItem
        {
            ModuleId = module.Id,
            Word = word,
            NormalizedWord = NormalizeWord(word),
            BmText = bmText,
            ZhText = request.ZhText?.Trim(),
            EnText = request.EnText?.Trim(),
            Language = request.Language?.Trim() ?? module.Language,
            Subject = request.Subject?.Trim() ?? module.Subject,
            YearLevel = request.YearLevel ?? module.YearLevel,
            SyllablesJson = EnsureJsonArray(request.SyllablesJson),
            SyllableText = request.SyllableText?.Trim(),
            ItemType = string.IsNullOrWhiteSpace(request.ItemType) ? "WORD" : request.ItemType.Trim().ToUpperInvariant(),
            DisplayOrder = Math.Max(0, request.DisplayOrder ?? 0),
            PhoneticHint = request.PhoneticHint?.Trim(),
            PronunciationText = request.PronunciationText?.Trim(),
            DifficultyLevel = Math.Clamp(request.DifficultyLevel ?? 1, 1, 5),
            MeaningText = request.MeaningText?.Trim(),
            ExampleSentence = request.ExampleSentence?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            IsActive = true
        };

        dbContext.VocabularyItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    internal static SyllabusModuleDto ToDto(SyllabusModule module) => new(
        module.Id,
        module.PublicId,
        module.ModuleCode,
        module.Subject,
        module.Language,
        module.YearLevel,
        module.Term,
        module.Week,
        module.UnitNumber,
        module.UnitTitle,
        module.Title,
        module.Description,
        module.SourceType,
        module.IsActive);

    internal static VocabularyItemDto ToDto(VocabularyItem item) => new(
        item.Id,
        item.PublicId,
        item.ModuleId,
        item.Word,
        item.NormalizedWord,
        item.BmText,
        item.ZhText,
        item.EnText,
        item.Language,
        item.Subject,
        item.YearLevel,
        item.SyllablesJson,
        item.SyllableText,
        item.ItemType,
        item.DisplayOrder,
        item.PhoneticHint,
        item.PronunciationText,
        item.DifficultyLevel,
        item.MeaningText,
        item.ExampleSentence,
        item.ImageUrl,
        item.IsActive);

    internal static string NormalizeWord(string value) =>
        string.Join(' ', value.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{name} is required");

        return value.Trim();
    }

    private static string EnsureJsonArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "[]";

        try
        {
            var node = JsonNode.Parse(value);
            return node is JsonArray ? node.ToJsonString() : "[]";
        }
        catch
        {
            return "[]";
        }
    }
}

