using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Models.Common;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.ClassroomThumbnails;
using CleanArc.Application.Contracts.Infrastructure.Documents;
using CleanArc.Application.Contracts.Infrastructure.Rag;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Classrooms.Commands;
using CleanArc.Application.Features.Games.Commands;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;


namespace CleanArc.Tests.Setup.Features.Classrooms;

public class AiBackgroundJobTests
{
    private static ApplicationDbContext CreateContext()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = ":memory:" }.ToString());
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task ExecuteWeeklyReportJob_Success_UpdatesAuditLogAndUsage()
    {
        await using var context = CreateContext();
        var repo = new ClassroomRepository(context);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ClassroomRepository.Returns(repo);

        var teacher = await AddUserAsync(context, "teacher-weekly");
        var classroom = await AddClassroomAsync(context, teacher.Id, "Weekly Test Class");

        var analytics = Substitute.For<IAdaptiveAnalyticsService>();
        analytics.GetClassWeaknessOverviewAsync(classroom.Id, Arg.Any<CancellationToken>())
            .Returns(new ClassWeaknessOverviewDto(classroom.Id, 0, 0, Array.Empty<StudentWordMasteryDto>()));


        var aiGen = Substitute.For<IAiGenerationService>();
        aiGen.GenerateJsonAsync(Arg.Any<ChallengeGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<ChallengeGenerationResult>.SuccessResult(new ChallengeGenerationResult("Generated Markdown Response")));

        var aiAudit = Substitute.For<IAiAuditService>();
        var aiUsage = Substitute.For<IAiUsageService>();

        var handler = new GenerateWeeklyReportJobCommandHandler(
            unitOfWork,
            analytics,
            aiGen,
            aiAudit,
            aiUsage,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GenerateWeeklyReportJobCommandHandler>.Instance
        );

        var result = await handler.Handle(
            new GenerateWeeklyReportJobCommand(999, classroom.Id, teacher.Id),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        
        // Assertions verifying correct services are called
        await aiAudit.Received(1).CompleteAsync(
            999,
            "Generated Markdown Response",
            Arg.Any<string>(),
            AiValidationStatuses.Valid,
            Arg.Any<string[]>(),
            Arg.Any<CancellationToken>()
        );

        await aiUsage.Received(1).ConsumeUsageAsync(
            teacher.Id,
            AiFeatureTypes.WeeklyReportGeneration,
            Arg.Any<string>(),
            "GEMINI",
            Arg.Any<string>(),
            1,
            true,
            Arg.Any<string>(),
            "classroom",
            classroom.Id,
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ExecuteWeeklyReportJob_Failure_MarksAuditLogFailed()
    {
        await using var context = CreateContext();
        var repo = new ClassroomRepository(context);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ClassroomRepository.Returns(repo);

        var teacher = await AddUserAsync(context, "teacher-failed");
        var classroom = await AddClassroomAsync(context, teacher.Id, "Weekly Failed Class");

        var analytics = Substitute.For<IAdaptiveAnalyticsService>();
        analytics.GetClassWeaknessOverviewAsync(classroom.Id, Arg.Any<CancellationToken>())
            .Returns(new ClassWeaknessOverviewDto(classroom.Id, 0, 0, Array.Empty<StudentWordMasteryDto>()));
        var aiGen = Substitute.For<IAiGenerationService>();

        aiGen.GenerateJsonAsync(Arg.Any<ChallengeGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<ChallengeGenerationResult>.FailureResult("Gemini API Error"));

        var aiAudit = Substitute.For<IAiAuditService>();
        var aiUsage = Substitute.For<IAiUsageService>();

        var handler = new GenerateWeeklyReportJobCommandHandler(
            unitOfWork,
            analytics,
            aiGen,
            aiAudit,
            aiUsage,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GenerateWeeklyReportJobCommandHandler>.Instance
        );

        var result = await handler.Handle(
            new GenerateWeeklyReportJobCommand(999, classroom.Id, teacher.Id),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        
        await aiAudit.Received(1).FailAsync(
            999,
            null,
            Arg.Is<string[]>(errors => errors[0] == "Gemini API Error"),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ExecuteClassroomThumbnailJob_Success_InvokesHFAndCompletesAudit()
    {
        var hfGen = Substitute.For<IClassroomThumbnailImageGenerationService>();
        hfGen.GenerateAsync(Arg.Any<ClassroomThumbnailGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(OperationResult<(byte[], string, string)>.SuccessResult((new byte[] { 1, 2, 3 }, "image/png", "custom-hf-model")));

        var aiAudit = Substitute.For<IAiAuditService>();
        var aiUsage = Substitute.For<IAiUsageService>();
        aiUsage.GetRemainingQuotaAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiQuotaResult(100, 0, 100));

        var handler = new GenerateClassroomThumbnailJobCommandHandler(
            hfGen,
            aiAudit,
            aiUsage,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GenerateClassroomThumbnailJobCommandHandler>.Instance
        );


        var result = await handler.Handle(
            new GenerateClassroomThumbnailJobCommand(111, 10, "Class A", 1, new[] { "Maths" }, "Desc", "A simple illustration of books"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);

        await hfGen.Received(1).GenerateAsync(
            Arg.Is<ClassroomThumbnailGenerationRequest>(req => req.TeacherId == 10 && req.ClassroomName == "Class A"),
            Arg.Any<CancellationToken>()
        );

        await aiAudit.Received(1).CompleteAsync(
            111,
            Arg.Any<string>(),
            Arg.Any<string>(),
            AiValidationStatuses.Valid,
            Arg.Any<string[]>(),
            Arg.Any<CancellationToken>()
        );
    }

    private static async Task<User> AddUserAsync(ApplicationDbContext context, string userName)
    {
        var user = new User
        {
            UserName = userName,
            Email = $"{userName}@example.com",
            Name = userName,
            Experience = 1
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<Classroom> AddClassroomAsync(ApplicationDbContext context, int teacherId, string name)
    {
        var classroom = new Classroom
        {
            Name = name,
            Description = "Description",
            Subject = "Science",
            YearLevel = 1,
            TeacherId = teacherId,
            JoinCode = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            IsActive = true
        };
        context.Classrooms.Add(classroom);
        await context.SaveChangesAsync();
        return classroom;
    }
}
