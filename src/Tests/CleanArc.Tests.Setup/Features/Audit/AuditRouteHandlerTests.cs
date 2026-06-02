using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Audit;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Infrastructure.Persistence.Services.Audit;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Audit;

public class AuditRouteHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_UnknownIntent_ReturnsNull()
    {
        var handler = CreateHandler();

        var result = await handler.TryHandleAsync(
            new AuditRouterResult(AuditIntentTypes.Unknown, new AuditRouteParameters()),
            new AuditRouteRequest(1, 1, "unknown", Array.Empty<AuditRouteUserContext>()),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryHandleAsync_WeakWordAnalysis_ReturnsStructuredJson()
    {
        var auditService = Substitute.For<IAuditService>();
        var classroomRepository = Substitute.For<IClassroomRepository>();
        var analytics = Substitute.For<IAdaptiveAnalyticsService>();

        classroomRepository.GetTeacherClassroomsAsync(1).Returns(new List<Classroom>
        {
            SetId(new Classroom { Name = "Primary 4A", TeacherId = 1, IsActive = true, IsDeleted = false }, 10)
        });

        auditService.GetWeakWordsAsync(10, null, Arg.Any<CancellationToken>())
            .Returns(new WeakWordsAuditDto(10, null, ["keluarga", "membantu"], 5));

        var handler = new AuditRouteHandler(auditService, classroomRepository, analytics);

        var result = await handler.TryHandleAsync(
            new AuditRouterResult(AuditIntentTypes.WeakWordAnalysis, new AuditRouteParameters()),
            new AuditRouteRequest(1, 1, "Which words are causing problems?", Array.Empty<AuditRouteUserContext>()),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(AuditIntentTypes.WeakWordAnalysis, result.Intent);
        Assert.Contains("keluarga", result.AnswerJson);
        Assert.Contains("audit_router", result.AnswerJson);
    }

    [Fact]
    public async Task TryHandleAsync_StudentPerformance_ReturnsMatchedStrugglingStudents()
    {
        var auditService = Substitute.For<IAuditService>();
        var classroomRepository = Substitute.For<IClassroomRepository>();
        var analytics = Substitute.For<IAdaptiveAnalyticsService>();

        classroomRepository.GetTeacherClassroomsAsync(1).Returns(new List<Classroom>
        {
            SetId(new Classroom { Name = "Primary 4A", TeacherId = 1, IsActive = true, IsDeleted = false }, 10)
        });
        classroomRepository.GetClassroomMembersAsync(10).Returns(new List<ClassroomStudent>
        {
            new() { ClassroomId = 10, UserId = 100 },
            new() { ClassroomId = 10, UserId = 101 }
        });

        auditService.GetStudentPerformanceAsync(100, Arg.Any<CancellationToken>())
            .Returns(new StudentPerformanceAuditDto(100, null, 3, 1, 50m, ["a"], 5, 2));
        auditService.GetStudentPerformanceAsync(101, Arg.Any<CancellationToken>())
            .Returns(new StudentPerformanceAuditDto(101, null, 0, 0, 90m, [], 8, 7));

        var handler = new AuditRouteHandler(auditService, classroomRepository, analytics);

        var result = await handler.TryHandleAsync(
            new AuditRouterResult(AuditIntentTypes.StudentPerformance, new AuditRouteParameters()),
            new AuditRouteRequest(
                1,
                1,
                "Show struggling students",
                new List<AuditRouteUserContext>
                {
                    new(100, "student_a", "student", "Primary 4A"),
                    new(101, "student_b", "student", "Primary 4A")
                }),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(AuditIntentTypes.StudentPerformance, result.Intent);
        Assert.Contains(100, result.MatchedUserIds);
        Assert.DoesNotContain(101, result.MatchedUserIds);
    }

    private static AuditRouteHandler CreateHandler()
    {
        return new AuditRouteHandler(
            Substitute.For<IAuditService>(),
            Substitute.For<IClassroomRepository>(),
            Substitute.For<IAdaptiveAnalyticsService>());
    }

    private static T SetId<T>(T entity, int id)
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}
