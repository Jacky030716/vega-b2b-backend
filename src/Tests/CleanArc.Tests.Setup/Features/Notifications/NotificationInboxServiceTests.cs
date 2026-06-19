using CleanArc.Application.Contracts.Notifications;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.Notifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleanArc.Tests.Setup.Features.Notifications;

public class NotificationInboxServiceTests
{
    [Fact]
    public async Task GetLatestAsync_ReturnsOnlyOwnersNotificationsNewestFirst()
    {
        using var context = CreateContext();
        var firstUser = await AddUserAsync(context, "first-user");
        var secondUser = await AddUserAsync(context, "second-user");
        var service = new NotificationInboxService(context);

        var older = await service.CreateAsync(CreateRequest(firstUser.Id, "Older"), CancellationToken.None);
        var newer = await service.CreateAsync(CreateRequest(firstUser.Id, "Newer"), CancellationToken.None);
        await service.CreateAsync(CreateRequest(secondUser.Id, "Other user"), CancellationToken.None);

        var olderRow = await context.UserNotifications.FindAsync(older.Id);
        var newerRow = await context.UserNotifications.FindAsync(newer.Id);
        olderRow!.CreatedTime = DateTime.UtcNow.AddMinutes(-5);
        newerRow!.CreatedTime = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var results = await service.GetLatestAsync(firstUser.Id, 100, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("Newer", results[0].Title);
        Assert.Equal("Older", results[1].Title);
    }

    [Fact]
    public async Task GetByIdAsync_RespectsOwnership()
    {
        using var context = CreateContext();
        var owner = await AddUserAsync(context, "owner");
        var otherUser = await AddUserAsync(context, "other-user");
        var service = new NotificationInboxService(context);
        var notification = await service.CreateAsync(CreateRequest(owner.Id, "Owned"), CancellationToken.None);

        var owned = await service.GetByIdAsync(notification.Id, owner.Id, CancellationToken.None);
        var otherResult = await service.GetByIdAsync(notification.Id, otherUser.Id, CancellationToken.None);

        Assert.NotNull(owned);
        Assert.Equal(notification.Id, owned!.Id);
        Assert.Null(otherResult);
    }

    [Fact]
    public async Task ReadAndDelete_RequireOwnershipAndReadIsIdempotent()
    {
        using var context = CreateContext();
        var owner = await AddUserAsync(context, "owner");
        var otherUser = await AddUserAsync(context, "other-user");
        var service = new NotificationInboxService(context);
        var notification = await service.CreateAsync(CreateRequest(owner.Id, "Owned"), CancellationToken.None);

        Assert.False(await service.MarkAsReadAsync(notification.Id, otherUser.Id, CancellationToken.None));
        Assert.True(await service.MarkAsReadAsync(notification.Id, owner.Id, CancellationToken.None));

        var firstReadAt = (await context.UserNotifications.FindAsync(notification.Id))!.ReadAt;
        Assert.NotNull(firstReadAt);
        Assert.True(await service.MarkAsReadAsync(notification.Id, owner.Id, CancellationToken.None));
        Assert.Equal(firstReadAt, (await context.UserNotifications.FindAsync(notification.Id))!.ReadAt);

        Assert.False(await service.DeleteAsync(notification.Id, otherUser.Id, CancellationToken.None));
        Assert.True(await service.DeleteAsync(notification.Id, owner.Id, CancellationToken.None));
        Assert.Null(await context.UserNotifications.FindAsync(notification.Id));
    }

    [Fact]
    public async Task CreateAsync_WithSameDeduplicationKey_ReturnsExistingNotification()
    {
        using var context = CreateContext();
        var user = await AddUserAsync(context, "dedup-user");
        var service = new NotificationInboxService(context);
        var request = CreateRequest(user.Id, "Review", "srs-overdue:123");

        var first = await service.CreateAsync(request, CancellationToken.None);
        var second = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await context.UserNotifications.CountAsync());
    }

    private static CreateNotificationRequest CreateRequest(
        int userId,
        string title,
        string? deduplicationKey = null) => new(
        userId,
        title,
        "Notification body",
        "SYSTEM_B2B",
        "{\"link\":\"/(educator)/classrooms\"}",
        deduplicationKey);

    private static ApplicationDbContext CreateContext()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = ":memory:" }.ToString());
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var context = new ApplicationDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<User> AddUserAsync(ApplicationDbContext context, string userName)
    {
        var user = new User { Name = userName, UserName = userName };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}
