using System.Net;
using System.Text.Json;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Adaptive;
using CleanArc.Infrastructure.Persistence.Services.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CleanArc.Tests.Setup.Features.Notifications;

public class SrsNotificationServiceTests
{
    [Fact]
    public async Task SendNotificationIfOverdueAsync_WithoutPushToken_PersistsGroupedSnapshot()
    {
        using var context = CreateContext();
        var fixture = await AddOverdueReviewFixtureAsync(context, "no-push", null);
        var handler = new RecordingHttpMessageHandler();
        var service = CreateService(context, handler);

        await service.SendNotificationIfOverdueAsync(fixture.Student.Id, CancellationToken.None);
        await service.SendNotificationIfOverdueAsync(fixture.Student.Id, CancellationToken.None);

        var notification = await context.UserNotifications.SingleAsync();
        Assert.Equal(fixture.Student.Id, notification.UserId);
        Assert.Equal("ACADEMIC_CRITICAL", notification.AlertType);
        Assert.Contains("reviewGroups", notification.PayloadJson);
        Assert.Contains("\"link\":\"/(student)/notification-review", notification.PayloadJson);
        Assert.Equal(0, handler.RequestCount);

        using var payload = JsonDocument.Parse(notification.PayloadJson);
        Assert.Equal(3, payload.RootElement.GetProperty("overdueCount").GetInt32());
        Assert.Equal(2, payload.RootElement.GetProperty("moduleCount").GetInt32());

        var reviewGroups = payload.RootElement
            .GetProperty("reviewGroups")
            .EnumerateArray()
            .ToArray();

        Assert.Equal(2, reviewGroups.Length);

        var sharedGroup = reviewGroups.Single(group =>
            group.GetProperty("moduleId").GetInt32() == fixture.SharedModule.Id);
        Assert.Equal(fixture.FirstClassroom.Id, sharedGroup.GetProperty("classroomId").GetInt32());
        Assert.Equal("Animals", sharedGroup.GetProperty("moduleTitle").GetString());
        Assert.Equal(
            new[] { "cat", "dog" },
            sharedGroup.GetProperty("words")
                .EnumerateArray()
                .Select(word => word.GetProperty("word").GetString())
                .ToArray());

        var fruitGroup = reviewGroups.Single(group =>
            group.GetProperty("moduleId").GetInt32() == fixture.SecondModule.Id);
        Assert.Equal(fixture.SecondClassroom.Id, fruitGroup.GetProperty("classroomId").GetInt32());
        Assert.Equal(new[] { "apple" }, fruitGroup.GetProperty("words")
            .EnumerateArray()
            .Select(word => word.GetProperty("word").GetString())
            .ToArray());
    }

    [Fact]
    public async Task SendNotificationIfOverdueAsync_WithPushToken_IncludesPersistedAlertIdAndDetailRoute()
    {
        using var context = CreateContext();
        var fixture = await AddOverdueReviewFixtureAsync(context, "with-push", "ExponentPushToken[test]");
        var handler = new RecordingHttpMessageHandler();
        var service = CreateService(context, handler);

        await service.SendNotificationIfOverdueAsync(fixture.Student.Id, CancellationToken.None);

        var notification = await context.UserNotifications.SingleAsync();
        Assert.Equal(1, handler.RequestCount);
        Assert.Contains($"\"alertId\":{notification.Id}", handler.RequestBody);
        Assert.Contains($"/(student)/notification-review/{notification.Id}", handler.RequestBody);
        Assert.Contains("\"reviewGroups\"", handler.RequestBody);
    }

    [Fact]
    public async Task SendNotificationIfOverdueAsync_SkipsWhenInAppNotificationsAreDisabled()
    {
        using var context = CreateContext();
        var fixture = await AddOverdueReviewFixtureAsync(context, "disabled-master", null);
        fixture.Student.InAppNotificationsEnabled = false;
        await context.SaveChangesAsync();

        var handler = new RecordingHttpMessageHandler();
        var service = CreateService(context, handler);

        await service.SendNotificationIfOverdueAsync(fixture.Student.Id, CancellationToken.None);

        Assert.Empty(context.UserNotifications);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SendNotificationIfOverdueAsync_SkipsBeforeReminderTimeInUserTimezone()
    {
        using var context = CreateContext();
        var fixture = await AddOverdueReviewFixtureAsync(context, "before-time", null);
        fixture.Student.NotificationTimezone = "UTC";
        fixture.Student.ReminderTimeLocal = DateTime.UtcNow.AddHours(2).ToString("HH:mm");
        await context.SaveChangesAsync();

        var handler = new RecordingHttpMessageHandler();
        var service = CreateService(context, handler);

        await service.SendNotificationIfOverdueAsync(fixture.Student.Id, CancellationToken.None);

        Assert.Empty(context.UserNotifications);
        Assert.Equal(0, handler.RequestCount);
    }

    private static SrsNotificationService CreateService(
        ApplicationDbContext context,
        RecordingHttpMessageHandler handler) => new(
        context,
        new HttpClient(handler),
        new NotificationInboxService(context),
        NullLogger<SrsNotificationService>.Instance);

    private static ApplicationDbContext CreateContext()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = ":memory:" }.ToString());
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var context = new ApplicationDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<OverdueReviewFixture> AddOverdueReviewFixtureAsync(
        ApplicationDbContext context,
        string userName,
        string? pushToken)
    {
        var student = new User
        {
            Name = userName,
            UserName = userName,
            ExpoPushToken = pushToken,
            NotificationTimezone = "UTC",
            ReminderTimeLocal = DateTime.UtcNow.AddHours(-1).ToString("HH:mm"),
            QuietHoursStartLocal = "00:00",
            QuietHoursEndLocal = "00:00"
        };
        var teacher = new User
        {
            Name = $"{userName}-teacher",
            UserName = $"{userName}-teacher"
        };
        context.Users.AddRange(student, teacher);
        await context.SaveChangesAsync();

        var firstClassroom = new Classroom
        {
            Name = "Class 4A",
            Description = "First classroom",
            Subject = "English",
            YearLevel = 4,
            JoinCode = "A001",
            TeacherId = teacher.Id,
            Thumbnail = string.Empty,
            ThumbnailType = "DEFAULT"
        };
        var secondClassroom = new Classroom
        {
            Name = "Class 4B",
            Description = "Second classroom",
            Subject = "English",
            YearLevel = 4,
            JoinCode = "B001",
            TeacherId = teacher.Id,
            Thumbnail = string.Empty,
            ThumbnailType = "DEFAULT"
        };
        context.Classrooms.AddRange(firstClassroom, secondClassroom);
        await context.SaveChangesAsync();

        context.ClassroomStudents.AddRange(
            new ClassroomStudent { ClassroomId = firstClassroom.Id, UserId = student.Id },
            new ClassroomStudent { ClassroomId = secondClassroom.Id, UserId = student.Id });

        var sharedModule = new SyllabusModule
        {
            ModuleCode = $"shared-{userName}",
            Subject = "English",
            Language = "en",
            YearLevel = 4,
            Term = "1",
            UnitTitle = "Animals",
            Title = "Animals",
            Description = "Shared review module",
            SourceType = "seed"
        };
        var otherModule = new SyllabusModule
        {
            ModuleCode = $"other-{userName}",
            Subject = "English",
            Language = "en",
            YearLevel = 4,
            Term = "1",
            UnitTitle = "Fruits",
            Title = "Fruits",
            Description = "Secondary review module",
            SourceType = "seed"
        };
        context.SyllabusModules.AddRange(sharedModule, otherModule);
        await context.SaveChangesAsync();

        context.ClassroomModules.AddRange(
            new ClassroomModule { ClassroomId = firstClassroom.Id, ModuleId = sharedModule.Id },
            new ClassroomModule { ClassroomId = secondClassroom.Id, ModuleId = sharedModule.Id },
            new ClassroomModule { ClassroomId = secondClassroom.Id, ModuleId = otherModule.Id });
        await context.SaveChangesAsync();

        var cat = await AddVocabularyItemAsync(context, sharedModule, "cat", 1);
        var dog = await AddVocabularyItemAsync(context, sharedModule, "dog", 2);
        var apple = await AddVocabularyItemAsync(context, otherModule, "apple", 1);

        context.WordProgresses.AddRange(
            new WordProgress
            {
                StudentId = student.Id,
                WordId = cat.Id,
                NextReviewDate = DateTime.UtcNow.AddMinutes(-30)
            },
            new WordProgress
            {
                StudentId = student.Id,
                WordId = dog.Id,
                NextReviewDate = DateTime.UtcNow.AddMinutes(-20)
            },
            new WordProgress
            {
                StudentId = student.Id,
                WordId = apple.Id,
                NextReviewDate = DateTime.UtcNow.AddMinutes(-10)
            });
        await context.SaveChangesAsync();

        return new OverdueReviewFixture(student, firstClassroom, secondClassroom, sharedModule, otherModule);
    }

    private static async Task<VocabularyItem> AddVocabularyItemAsync(
        ApplicationDbContext context,
        SyllabusModule module,
        string word,
        int displayOrder)
    {
        var item = new VocabularyItem
        {
            ModuleId = module.Id,
            Word = word,
            NormalizedWord = word,
            Language = "en",
            Subject = module.Subject,
            YearLevel = module.YearLevel,
            DisplayOrder = displayOrder
        };

        context.VocabularyItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    private sealed record OverdueReviewFixture(
        User Student,
        Classroom FirstClassroom,
        Classroom SecondClassroom,
        SyllabusModule SharedModule,
        SyllabusModule SecondModule);

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
