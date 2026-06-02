using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Audit;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Audit;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Audit;

public class AuditServiceTests
{
    [Fact]
    public async Task GetWeakWordsAsync_AggregatesDistinctWordsAndAffectedStudents()
    {
        var analytics = Substitute.For<IAdaptiveAnalyticsService>();
        var classroomRepository = Substitute.For<IClassroomRepository>();

        analytics.GetClassWeaknessOverviewAsync(10, Arg.Any<CancellationToken>())
            .Returns(new ClassWeaknessOverviewDto(
                10,
                3,
                1,
                new List<StudentWordMasteryDto>
                {
                    CreateWeakWord(1, "keluarga", 40),
                    CreateWeakWord(2, "keluarga", 45),
                    CreateWeakWord(2, "membantu", 50),
                    CreateWeakWord(3, "membantu", 55)
                }));

        var service = new AuditService(analytics, classroomRepository);

        var result = await service.GetWeakWordsAsync(10, null, CancellationToken.None);

        Assert.Equal(10, result.ClassroomId);
        Assert.Null(result.ModuleId);
        Assert.Equal(["keluarga", "membantu"], result.WeakWords);
        Assert.Equal(3, result.AffectedStudents);
    }

    [Fact]
    public async Task GetWeakWordsAsync_WithModuleId_UsesModuleWeaknessOverview()
    {
        var analytics = Substitute.For<IAdaptiveAnalyticsService>();
        var classroomRepository = Substitute.For<IClassroomRepository>();

        analytics.GetModuleWeaknessOverviewAsync(10, 5, Arg.Any<CancellationToken>())
            .Returns(new ModuleWeaknessOverviewDto(
                10,
                5,
                1,
                0,
                new List<StudentWordMasteryDto> { CreateWeakWord(1, "saya", 30) }));

        var service = new AuditService(analytics, classroomRepository);

        var result = await service.GetWeakWordsAsync(10, 5, CancellationToken.None);

        Assert.Equal(5, result.ModuleId);
        Assert.Equal(["saya"], result.WeakWords);
        Assert.Equal(1, result.AffectedStudents);
        await analytics.DidNotReceive()
            .GetClassWeaknessOverviewAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetClassroomHealthAsync_ReturnsNotStarted_WhenNoStudents()
    {
        var analytics = Substitute.For<IAdaptiveAnalyticsService>();
        var classroomRepository = Substitute.For<IClassroomRepository>();

        classroomRepository.GetStudentCountAsync(7).Returns(0);
        analytics.GetClassWeaknessOverviewAsync(7, Arg.Any<CancellationToken>())
            .Returns(new ClassWeaknessOverviewDto(7, 0, 0, Array.Empty<StudentWordMasteryDto>()));
        analytics.GetModuleProgressSummaryAsync(7, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ModuleProgressSummaryDto>());

        var service = new AuditService(analytics, classroomRepository);

        var result = await service.GetClassroomHealthAsync(7, CancellationToken.None);

        Assert.Equal(AuditHealthStatuses.NotStarted, result.Status);
        Assert.Equal(0, result.StudentCount);
    }

    [Fact]
    public async Task GetClassroomHealthAsync_ReturnsNeedsReview_WhenWeakWordsExist()
    {
        var analytics = Substitute.For<IAdaptiveAnalyticsService>();
        var classroomRepository = Substitute.For<IClassroomRepository>();

        classroomRepository.GetStudentCountAsync(7).Returns(12);
        analytics.GetClassWeaknessOverviewAsync(7, Arg.Any<CancellationToken>())
            .Returns(new ClassWeaknessOverviewDto(7, 4, 2, Array.Empty<StudentWordMasteryDto>()));
        analytics.GetModuleProgressSummaryAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleProgressSummaryDto>
            {
                new(7, 1, "Unit 1", "Malay", 4, 10, 2, 1, 50, 0, 72.5m, null, "IN_PROGRESS")
            });

        var service = new AuditService(analytics, classroomRepository);

        var result = await service.GetClassroomHealthAsync(7, CancellationToken.None);

        Assert.Equal(AuditHealthStatuses.NeedsReview, result.Status);
        Assert.Equal(4, result.WeakWordCount);
        Assert.Equal(72.5m, result.AverageMasteryScore);
    }

    [Fact]
    public async Task GetClassroomHealthAsync_ReturnsHealthy_WhenNoWeakWordsOrReviewModules()
    {
        var analytics = Substitute.For<IAdaptiveAnalyticsService>();
        var classroomRepository = Substitute.For<IClassroomRepository>();

        classroomRepository.GetStudentCountAsync(7).Returns(8);
        analytics.GetClassWeaknessOverviewAsync(7, Arg.Any<CancellationToken>())
            .Returns(new ClassWeaknessOverviewDto(7, 0, 0, Array.Empty<StudentWordMasteryDto>()));
        analytics.GetModuleProgressSummaryAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleProgressSummaryDto>
            {
                new(7, 1, "Unit 1", "Malay", 4, 10, 2, 2, 100, 0, 90m, null, "COMPLETED")
            });

        var service = new AuditService(analytics, classroomRepository);

        var result = await service.GetClassroomHealthAsync(7, CancellationToken.None);

        Assert.Equal(AuditHealthStatuses.Healthy, result.Status);
        Assert.Equal(0, result.ModulesNeedingReviewCount);
    }

    [Fact]
    public async Task GetStudentPerformanceAsync_MapsStructuredMetricsOnly()
    {
        var analytics = Substitute.For<IAdaptiveAnalyticsService>();
        var classroomRepository = Substitute.For<IClassroomRepository>();

        analytics.GetStudentPerformanceSummaryAsync(99, Arg.Any<CancellationToken>())
            .Returns(new StudentPerformanceSummaryDto(
                99,
                new List<WordMasterySummaryDto>
                {
                    new(1, 99, 1, 1, "keluarga", 80, "strong", 5, 4, null, null, false, "[]", null),
                    new(2, 99, 2, 1, "membantu", 50, "weak", 3, 1, null, null, false, "[]", null)
                },
                new WeaknessSummaryDto(
                    99,
                    1,
                    2,
                    new List<StudentWordMasteryDto> { CreateWeakWord(99, "membantu", 50) },
                    new List<string> { "spell_catcher" }),
                new List<ChallengeAttemptSummaryDto>
                {
                    new(1, 99, 2, 1, 1, 1, 4, 80, 3, DateTime.UtcNow)
                },
                Array.Empty<AdaptiveRecommendationDto>()));

        var service = new AuditService(analytics, classroomRepository);

        var result = await service.GetStudentPerformanceAsync(99, CancellationToken.None);

        Assert.Equal(99, result.StudentId);
        Assert.Equal(1, result.WeakWordCount);
        Assert.Equal(2, result.OverdueReviewCount);
        Assert.Equal(65m, result.AverageMasteryScore);
        Assert.Equal(["membantu"], result.WeakWords);
        Assert.Equal(3, result.AttemptCount);
        Assert.Equal(2, result.CompletedChallengeCount);
    }

    private static StudentWordMasteryDto CreateWeakWord(int studentId, string word, int masteryScore) =>
        new(
            Id: studentId,
            StudentId: studentId,
            VocabularyItemId: studentId,
            ModuleId: 1,
            Word: word,
            MasteryScore: masteryScore,
            MasteryLevel: "weak",
            TotalAttempts: 3,
            CorrectAttempts: 1,
            LastPracticedAt: null,
            NextReviewAt: null,
            WeaknessTagsJson: "[]",
            IsDueForReview: false,
            ErrorPatternsJson: null);
}
