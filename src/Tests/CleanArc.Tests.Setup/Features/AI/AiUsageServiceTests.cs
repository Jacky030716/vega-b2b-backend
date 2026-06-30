using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Domain.Entities.Institution;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using CleanArc.Infrastructure.Persistence.Services.AI;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CleanArc.Tests.Setup.Features.AI;

public class AiUsageServiceTests
{
  private static ApplicationDbContext CreateContext(SqliteConnection connection)
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite(connection)
      .Options;

    var context = new ApplicationDbContext(options);
    context.Database.OpenConnection();
    context.Database.EnsureCreated();
    return context;
  }

  [Fact]
  public async Task GetRemainingQuotaAsync_ReturnsAdminTier_ForAdminRole()
  {
    using var connection = new SqliteConnection("DataSource=:memory:");
    await connection.OpenAsync();

    await using var context = CreateContext(connection);
    var institution = new Institution { Name = "Vega Primary", SubscriptionTier = "Premium" };
    var adminRole = new Role { Name = "admin", NormalizedName = "ADMIN", DisplayName = "Admin" };
    var user = new User { UserName = "admin01", Name = "Vega Admin", Institution = institution };

    context.Institutions.Add(institution);
    context.Roles.Add(adminRole);
    context.Users.Add(user);
    await context.SaveChangesAsync();

    context.UserRoles.Add(new UserRole
    {
      UserId = user.Id,
      RoleId = adminRole.Id,
      User = user,
      Role = adminRole,
    });
    await context.SaveChangesAsync();

    var service = new AiUsageService(
      context,
      Options.Create(new AiUsageLimitOptions()),
      NullLogger<AiUsageService>.Instance);

    var quota = await service.GetRemainingQuotaAsync(user.Id, AiFeatureTypes.CustomChallengeGeneration, CancellationToken.None);

    Assert.Equal(600, quota.MonthlyLimit);
    Assert.Equal(600, quota.Remaining);
  }

  [Fact]
  public async Task GetRemainingQuotaAsync_UsesInstitutionPremium_ForNonAdmin()
  {
    using var connection = new SqliteConnection("DataSource=:memory:");
    await connection.OpenAsync();

    await using var context = CreateContext(connection);
    var institution = new Institution { Name = "Vega Primary", SubscriptionTier = "Premium" };
    var user = new User { UserName = "teacher01", Name = "Teacher", Institution = institution };

    context.Institutions.Add(institution);
    context.Users.Add(user);
    await context.SaveChangesAsync();

    var service = new AiUsageService(
      context,
      Options.Create(new AiUsageLimitOptions()),
      NullLogger<AiUsageService>.Instance);

    var quota = await service.GetRemainingQuotaAsync(user.Id, AiFeatureTypes.CustomChallengeGeneration, CancellationToken.None);

    Assert.Equal(300, quota.MonthlyLimit);
    Assert.Equal(300, quota.Remaining);
  }
}
