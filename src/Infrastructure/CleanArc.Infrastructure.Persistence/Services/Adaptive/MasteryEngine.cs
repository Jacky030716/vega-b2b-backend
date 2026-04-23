using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;
public class MasteryEngine(ApplicationDbContext dbContext) : IMasteryEngine
{
    public async Task<StudentWordMasteryDto?> ApplyItemAttemptAsync(
        SubmitAdaptiveItemAttemptRequest request,
        CancellationToken cancellationToken)
    {
        var attempt = await dbContext.StudentChallengeAttempts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.StudentChallengeAttemptId, cancellationToken);

        if (attempt is null || request.VocabularyItemId is null)
            return null;

        var vocabulary = await dbContext.VocabularyItems.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VocabularyItemId.Value, cancellationToken);

        if (vocabulary is null)
            return null;

        var mastery = await dbContext.StudentWordMasteries
            .FirstOrDefaultAsync(m => m.StudentId == attempt.StudentId && m.VocabularyItemId == vocabulary.Id, cancellationToken);

        if (mastery is null)
        {
            mastery = new StudentWordMastery
            {
                StudentId = attempt.StudentId,
                VocabularyItemId = vocabulary.Id,
                ModuleId = vocabulary.ModuleId,
                MasteryScore = 0,
                MasteryLevel = "NEW"
            };
            dbContext.StudentWordMasteries.Add(mastery);
        }

        var delta = CalculateDelta(request);
        mastery.MasteryScore = Math.Clamp(mastery.MasteryScore + delta, 0, 100);
        mastery.MasteryLevel = ToMasteryLevel(mastery.MasteryScore);
        mastery.TotalAttempts += 1;
        mastery.CorrectAttempts += request.WasCorrect ? 1 : 0;
        mastery.FirstTryCorrectCount += request.FirstAttemptCorrect ? 1 : 0;
        mastery.TotalHintsUsed += Math.Max(0, request.HintsUsed);
        mastery.TotalRetries += Math.Max(0, request.RetriesCount);
        mastery.AverageResponseTimeMs = RollingAverage(mastery.AverageResponseTimeMs, request.ResponseTimeMs, mastery.TotalAttempts);
        mastery.LastPracticedAt = DateTime.UtcNow;
        mastery.NextReviewAt = CalculateNextReviewAt(mastery.MasteryLevel, request.WasCorrect);
        mastery.WeaknessTagsJson = BuildWeaknessTags(request, mastery.MasteryScore);
        mastery.LastGameTemplateId = request.GameTemplateId;

        await UpdateSkillProfileAsync(attempt.StudentId, vocabulary, request, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(mastery, vocabulary.Word);
    }

    internal static int CalculateDelta(SubmitAdaptiveItemAttemptRequest request)
    {
        var delta = request.WasCorrect ? 8 : -8;
        if (request.WasCorrect && request.FirstAttemptCorrect) delta += 4;
        if (request.WasCorrect && request.ResponseTimeMs is <= 5000) delta += 2;
        if (request.WasCorrect && request.ResponseTimeMs is >= 15000) delta += 5;
        delta -= Math.Min(Math.Max(0, request.HintsUsed) * 2, 6);
        delta -= Math.Min(Math.Max(0, request.RetriesCount) * 3, 9);
        return delta;
    }

    internal static string ToMasteryLevel(int score) => score switch
    {
        < 20 => "NEW",
        < 40 => "WEAK",
        < 65 => "LEARNING",
        < 85 => "REVIEW",
        _ => "MASTERED"
    };

    internal static DateTime CalculateNextReviewAt(string level, bool wasCorrect)
    {
        var now = DateTime.UtcNow;
        if (!wasCorrect || level is "NEW" or "WEAK") return now.AddDays(1);
        if (level == "LEARNING") return now.AddDays(2);
        if (level == "REVIEW") return now.AddDays(4);
        return now.AddDays(7);
    }

    internal static StudentWordMasteryDto ToDto(StudentWordMastery mastery, string word) => new(
        mastery.Id,
        mastery.StudentId,
        mastery.VocabularyItemId,
        mastery.ModuleId,
        word,
        mastery.MasteryScore,
        mastery.MasteryLevel,
        mastery.TotalAttempts,
        mastery.CorrectAttempts,
        mastery.LastPracticedAt,
        mastery.NextReviewAt,
        mastery.WeaknessTagsJson);

    private static int? RollingAverage(int? currentAverage, int? nextValue, int count)
    {
        if (nextValue is null) return currentAverage;
        if (currentAverage is null || count <= 1) return nextValue;
        return (int)Math.Round(((currentAverage.Value * (count - 1)) + nextValue.Value) / (decimal)count);
    }

    private static string BuildWeaknessTags(SubmitAdaptiveItemAttemptRequest request, int masteryScore)
    {
        var tags = new List<string>();
        if (!request.WasCorrect || masteryScore < 65) tags.Add("spelling_recall");
        if (request.ErrorType?.Contains("syllable", StringComparison.OrdinalIgnoreCase) == true) tags.Add("syllable_assembly");
        if (request.ErrorType?.Contains("pronunciation", StringComparison.OrdinalIgnoreCase) == true) tags.Add("pronunciation_recall");
        if (request.HintsUsed > 0) tags.Add("hint_dependency");
        if (request.RetriesCount > 0) tags.Add("retry_dependency");
        return JsonSerializer.Serialize(tags.Distinct(), ChallengeGenerator.JsonOptions);
    }

    private async Task UpdateSkillProfileAsync(
        int studentId,
        VocabularyItem vocabulary,
        SubmitAdaptiveItemAttemptRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.StudentSkillProfiles
            .FirstOrDefaultAsync(p => p.StudentId == studentId && p.Subject == vocabulary.Subject && p.Language == vocabulary.Language, cancellationToken);

        if (profile is null)
        {
            profile = new StudentSkillProfile
            {
                StudentId = studentId,
                Subject = vocabulary.Subject,
                Language = vocabulary.Language,
                SpellingRecallScore = 50,
                PronunciationRecallScore = 50,
                SyllableAssemblyScore = 50,
                VisualMemoryScore = 50,
                AuditoryMemoryScore = 50
            };
            dbContext.StudentSkillProfiles.Add(profile);
        }

        var nudge = request.WasCorrect ? 2 : -4;
        profile.SpellingRecallScore = Math.Clamp(profile.SpellingRecallScore + nudge, 0, 100);
        if (request.ErrorType?.Contains("syllable", StringComparison.OrdinalIgnoreCase) == true)
            profile.SyllableAssemblyScore = Math.Clamp(profile.SyllableAssemblyScore + nudge, 0, 100);
        if (request.ErrorType?.Contains("pronunciation", StringComparison.OrdinalIgnoreCase) == true)
            profile.PronunciationRecallScore = Math.Clamp(profile.PronunciationRecallScore + nudge, 0, 100);
        profile.ModifiedDate = DateTime.UtcNow;
    }
}

