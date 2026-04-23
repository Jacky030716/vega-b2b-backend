using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.User;
using CsvHelper;
using Mediator;
using System.Globalization;

namespace CleanArc.Application.Features.Admin.Commands.BulkCreateUsers;

internal sealed class BulkCreateUsersCommandHandler(IAppUserManager userManager) : IRequestHandler<BulkCreateUsersCommand, OperationResult<BulkCreateUsersResult>>
{
    private static readonly string[] VisualIconPool =
    [
        "icon_01", "icon_02", "icon_03", "icon_04",
        "icon_05", "icon_06", "icon_07", "icon_08",
        "icon_09", "icon_10", "icon_11", "icon_12"
    ];

    public async ValueTask<OperationResult<BulkCreateUsersResult>> Handle(BulkCreateUsersCommand request, CancellationToken cancellationToken)
    {
        if (request.Count <= 0 || request.Count > 1000)
            return OperationResult<BulkCreateUsersResult>.FailureResult("Count must be between 1 and 1000.");

        var role = request.Role?.ToLowerInvariant();
        if (role != "student" && role != "teacher")
            return OperationResult<BulkCreateUsersResult>.FailureResult("Role must be 'student' or 'teacher'.");

        var outputs = new List<BulkUserOutput>();
        var random = new Random();

        for (int i = 0; i < request.Count; i++)
        {
            var year = DateTime.UtcNow.Year;
            var shortId = Guid.NewGuid().ToString("N").Substring(0, 6);
            var username = $"vega_{role}_{year}_{shortId}";
            var password = role == "student" ? GenerateVisualPassword(random) : GenerateRandomTextPassword(random);

            var user = new User
            {
                UserName = username,
                Email = $"{username}@vega.demo",
                EmailConfirmed = true,
                InstitutionId = request.InstitutionId,
                Name = $"Bulk_{role}_{shortId}",
                FamilyName = "Generated"
            };

            var createResult = await userManager.CreateUserWithPasswordAsync(user, password);
            if (!createResult.Succeeded)
            {
                // In a robust implementation, we would collect errors. Here we fail fast.
                return OperationResult<BulkCreateUsersResult>.FailureResult($"Failed to create user {username}: {createResult.Errors.FirstOrDefault()?.Description}");
            }

            var roleResult = await userManager.AddUserToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                return OperationResult<BulkCreateUsersResult>.FailureResult($"Failed to assign role to {username}.");
            }

            outputs.Add(new BulkUserOutput
            {
                UserName = username,
                Password = password,
                Role = role
            });
        }

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(outputs);

        return OperationResult<BulkCreateUsersResult>.SuccessResult(new BulkCreateUsersResult
        {
            GeneratedCount = outputs.Count,
            CsvContent = writer.ToString()
        });
    }

    private static string GenerateVisualPassword(Random random)
    {
        var first = VisualIconPool[random.Next(VisualIconPool.Length)];
        var second = VisualIconPool[random.Next(VisualIconPool.Length)];
        var third = VisualIconPool[random.Next(VisualIconPool.Length)];
        return $"{first}-{second}-{third}";
    }

    private static string GenerateRandomTextPassword(Random random)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        return new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray()) + "A1!"; // satisfy generic policies
    }
}

public class BulkUserOutput
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
}
