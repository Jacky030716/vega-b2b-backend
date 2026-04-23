using System.Text.Json;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;

public class SyllabusModuleIngestionService(
    ApplicationDbContext dbContext,
    ILogger<SyllabusModuleIngestionService> logger) : ISyllabusModuleIngestionService
{
    public async Task<SyllabusIngestionResult> IngestAsync(
        SyllabusSeedDocument document,
        CancellationToken cancellationToken)
    {
        var counters = new IngestionCounters();
        var logs = new List<string>();
        var errors = new List<string>();

        var sourceType = string.IsNullOrWhiteSpace(document.SourceType)
            ? "PREDEFINED_OFFICIAL_SEED"
            : document.SourceType.Trim();
        var rawSubject = string.IsNullOrWhiteSpace(document.Subject)
            ? "Bahasa Melayu"
            : document.Subject.Trim();
        var subject = SyllabusSubjectMapper.NormalizeSubject(rawSubject) ?? rawSubject;

        foreach (var seedModule in document.Modules ?? Array.Empty<SyllabusSeedModule>())
        {
            var moduleCode = NormalizeCode(seedModule.ModuleCode);
            if (string.IsNullOrWhiteSpace(moduleCode))
            {
                errors.Add("Module rejected: moduleCode is required.");
                continue;
            }

            var module = await dbContext.SyllabusModules
                .FirstOrDefaultAsync(m => m.ModuleCode == moduleCode, cancellationToken);

            var isNewModule = module is null;
            module ??= new SyllabusModule
            {
                PublicId = Guid.NewGuid(),
                ModuleCode = moduleCode
            };

            module.Subject = subject;
            module.Language = string.IsNullOrWhiteSpace(seedModule.Language?.Primary)
                ? "ms"
                : seedModule.Language.Primary.Trim();
            module.YearLevel = module.YearLevel <= 0 ? 1 : module.YearLevel;
            module.UnitNumber = seedModule.UnitNumber;
            module.UnitTitle = seedModule.UnitTitle?.Trim() ?? string.Empty;
            module.Title = string.IsNullOrWhiteSpace(seedModule.UnitTitle)
                ? moduleCode
                : seedModule.UnitTitle.Trim();
            module.Description = $"Predefined syllabus module {moduleCode}.";
            module.SourceType = sourceType;
            module.IsActive = true;

            if (isNewModule)
            {
                dbContext.SyllabusModules.Add(module);
                counters.ModulesCreated++;
                logs.Add($"Module created: {moduleCode}");
            }
            else
            {
                counters.ModulesUpdated++;
                logs.Add($"Module updated: {moduleCode}");
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await UpsertItemsAsync(seedModule, module, counters, logs, errors, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var line in logs)
            logger.LogInformation("Syllabus ingestion: {Message}", line);
        foreach (var error in errors)
            logger.LogWarning("Syllabus ingestion skipped row: {Message}", error);

        return new SyllabusIngestionResult(
            counters.ModulesCreated,
            counters.ModulesUpdated,
            counters.ItemsCreated,
            counters.ItemsUpdated,
            counters.ItemsRejected,
            logs,
            errors);
    }

    private async Task UpsertItemsAsync(
        SyllabusSeedModule seedModule,
        SyllabusModule module,
        IngestionCounters counters,
        List<string> logs,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        foreach (var seedItem in seedModule.Items ?? Array.Empty<SyllabusSeedItem>())
        {
            var bmText = seedItem.Text?.Ms?.Trim();
            if (string.IsNullOrWhiteSpace(bmText))
            {
                counters.ItemsRejected++;
                errors.Add($"{module.ModuleCode}: item rejected because bm_text is empty.");
                continue;
            }

            if (seedItem.DisplayOrder is null)
            {
                counters.ItemsRejected++;
                errors.Add($"{module.ModuleCode}/{bmText}: item rejected because displayOrder is missing.");
                continue;
            }

            var normalized = SyllabusModuleService.NormalizeWord(bmText);
            var item = await dbContext.VocabularyItems
                .FirstOrDefaultAsync(
                    v => v.ModuleId == module.Id && v.NormalizedWord == normalized,
                    cancellationToken);

            var isNewItem = item is null;
            item ??= new VocabularyItem
            {
                PublicId = Guid.NewGuid(),
                ModuleId = module.Id,
                NormalizedWord = normalized
            };

            item.Word = bmText;
            item.BmText = bmText;
            item.ZhText = seedItem.Text?.Zh?.Trim();
            item.EnText = seedItem.Text?.En?.Trim();
            item.Language = module.Language;
            item.Subject = module.Subject;
            item.YearLevel = module.YearLevel;
            item.SyllablesJson = JsonSerializer.Serialize(seedItem.Syllables ?? Array.Empty<string>());
            item.SyllableText = seedItem.SyllableText?.Trim();
            item.ItemType = string.IsNullOrWhiteSpace(seedItem.ItemType)
                ? "WORD"
                : seedItem.ItemType.Trim().ToUpperInvariant();
            item.DisplayOrder = seedItem.DisplayOrder.Value;
            item.DifficultyLevel = Math.Clamp(item.DifficultyLevel <= 0 ? 1 : item.DifficultyLevel, 1, 5);
            item.MeaningText = item.EnText;
            item.PhoneticHint = item.SyllableText;
            item.PronunciationText = bmText;
            item.IsActive = true;

            if (isNewItem)
            {
                dbContext.VocabularyItems.Add(item);
                counters.ItemsCreated++;
                logs.Add($"Item created: {module.ModuleCode}/{normalized}");
            }
            else
            {
                counters.ItemsUpdated++;
                logs.Add($"Item updated: {module.ModuleCode}/{normalized}");
            }
        }
    }

    private static string NormalizeCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    private sealed class IngestionCounters
    {
        public int ModulesCreated { get; set; }
        public int ModulesUpdated { get; set; }
        public int ItemsCreated { get; set; }
        public int ItemsUpdated { get; set; }
        public int ItemsRejected { get; set; }
    }
}
