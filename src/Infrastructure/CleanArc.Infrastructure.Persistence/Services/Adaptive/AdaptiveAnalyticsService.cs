using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services.Adaptive;
public class AdaptiveAnalyticsService(
    ApplicationDbContext dbContext,
    IRecommendationEngine recommendationEngine) : IAdaptiveAnalyticsService
{
    public async Task<IReadOnlyList<StudentWordMasteryDto>> GetMasteryAsync(int studentId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.StudentWordMasteries.AsNoTracking()
            .Include(m => m.VocabularyItem)
            .Where(m => m.StudentId == studentId)
            .OrderBy(m => m.MasteryScore)
            .ThenBy(m => m.NextReviewAt)
            .ToListAsync(cancellationToken);

        return rows.Select(m => MasteryEngine.ToDto(m, m.VocabularyItem.Word)).ToList();
    }

    public async Task<WeaknessSummaryDto> GetWeaknessSummaryAsync(int studentId, CancellationToken cancellationToken)
    {
        var mastery = await GetMasteryAsync(studentId, cancellationToken);
        var now = DateTime.UtcNow;
        var weak = mastery.Where(m => m.MasteryScore < 65).Take(20).ToList();
        var overdue = mastery.Count(m => m.NextReviewAt != null && m.NextReviewAt <= now);
        var recommended = await GetRecommendedNextChallengesAsync(studentId, cancellationToken);
        return new WeaknessSummaryDto(
            studentId,
            weak.Count,
            overdue,
            weak,
            recommended.Select(r => r.RecommendedGameTemplateCode).Distinct().ToList());
    }

    public Task<IReadOnlyList<AdaptiveRecommendationDto>> GetRecommendedNextChallengesAsync(int studentId, CancellationToken cancellationToken)
        => recommendationEngine.RecommendForStudentAsync(studentId, null, cancellationToken);

    public async Task<ClassWeaknessOverviewDto> GetClassWeaknessOverviewAsync(int classId, CancellationToken cancellationToken)
    {
        var studentIds = await dbContext.ClassroomStudents.AsNoTracking()
            .Where(cs => cs.ClassroomId == classId)
            .Select(cs => cs.UserId)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var weakRows = await dbContext.StudentWordMasteries.AsNoTracking()
            .Include(m => m.VocabularyItem)
            .Where(m => studentIds.Contains(m.StudentId) && m.MasteryScore < 65)
            .OrderBy(m => m.MasteryScore)
            .Take(50)
            .ToListAsync(cancellationToken);
        var weak = weakRows.Select(m => MasteryEngine.ToDto(m, m.VocabularyItem.Word)).ToList();

        var overdue = await dbContext.StudentWordMasteries.AsNoTracking()
            .CountAsync(m => studentIds.Contains(m.StudentId) && m.NextReviewAt != null && m.NextReviewAt <= now, cancellationToken);

        return new ClassWeaknessOverviewDto(classId, weak.Count, overdue, weak);
    }

    public async Task<IReadOnlyList<ModuleProgressDto>> GetModuleProgressAsync(int classId, CancellationToken cancellationToken)
    {
        var studentIds = await dbContext.ClassroomStudents.AsNoTracking()
            .Where(cs => cs.ClassroomId == classId)
            .Select(cs => cs.UserId)
            .ToListAsync(cancellationToken);

        var modules = await dbContext.SyllabusModules.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.YearLevel)
            .ThenBy(m => m.Week)
            .Take(50)
            .ToListAsync(cancellationToken);

        var result = new List<ModuleProgressDto>();
        foreach (var module in modules)
        {
            var vocabularyIds = await dbContext.VocabularyItems.AsNoTracking()
                .Where(v => v.ModuleId == module.Id && v.IsActive)
                .Select(v => v.Id)
                .ToListAsync(cancellationToken);

            var rows = await dbContext.StudentWordMasteries.AsNoTracking()
                .Where(m => studentIds.Contains(m.StudentId) && vocabularyIds.Contains(m.VocabularyItemId))
                .ToListAsync(cancellationToken);

            result.Add(new ModuleProgressDto(
                classId,
                module.Id,
                module.Title,
                vocabularyIds.Count,
                rows.Select(r => r.VocabularyItemId).Distinct().Count(),
                rows.Count == 0 ? 0 : Math.Round((decimal)rows.Average(r => r.MasteryScore), 2)));
        }

        return result;
    }

    public async Task<StudentPerformanceDto> GetStudentPerformanceAsync(int studentId, CancellationToken cancellationToken)
    {
        var mastery = await GetMasteryAsync(studentId, cancellationToken);
        var weakness = await GetWeaknessSummaryAsync(studentId, cancellationToken);
        var recommendations = await GetRecommendedNextChallengesAsync(studentId, cancellationToken);
        return new StudentPerformanceDto(studentId, mastery, weakness, recommendations);
    }
}

