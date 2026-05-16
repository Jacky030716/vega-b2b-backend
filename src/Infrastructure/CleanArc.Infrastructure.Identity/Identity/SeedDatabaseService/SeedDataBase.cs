using CleanArc.Domain.Entities.Institution;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Identity.Identity.Manager;
using CleanArc.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Identity.Identity.SeedDatabaseService;

public interface ISeedDataBase
{
    Task Seed();
}

public class SeedDataBase : ISeedDataBase
{
    private readonly AppUserManager _userManager;
    private readonly AppRoleManager _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public SeedDataBase(
        AppUserManager userManager,
        AppRoleManager roleManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public async Task Seed()
    {
        // Seed admin role
        if (!_roleManager.Roles.AsNoTracking().Any(r => r.Name.Equals("admin")))
        {
            var role = new Role
            {
                Name = "admin",
                DisplayName = "Administrator",
                CreatedDate = DateTime.UtcNow
            };
            await _roleManager.CreateAsync(role);
        }

        // Seed student role
        if (!_roleManager.Roles.AsNoTracking().Any(r => r.Name.Equals("student")))
        {
            var role = new Role
            {
                Name = "student",
                DisplayName = "Student",
                CreatedDate = DateTime.UtcNow
            };
            await _roleManager.CreateAsync(role);
        }

        // Seed teacher role
        if (!_roleManager.Roles.AsNoTracking().Any(r => r.Name.Equals("teacher")))
        {
            var role = new Role
            {
                Name = "teacher",
                DisplayName = "Teacher",
                CreatedDate = DateTime.UtcNow
            };
            await _roleManager.CreateAsync(role);
        }

        // Seed institution admin role
        if (!_roleManager.Roles.AsNoTracking().Any(r => r.Name.Equals("InstitutionAdmin")))
        {
            var role = new Role
            {
                Name = "InstitutionAdmin",
                DisplayName = "Institution Admin",
                CreatedDate = DateTime.UtcNow
            };
            await _roleManager.CreateAsync(role);
        }

        // Seed admin user
        if (!_userManager.Users.AsNoTracking().Any(u => u.UserName.Equals("admin")))
        {
            var user = new User
            {
                UserName = "admin",
                Email = "admin@site.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            await _userManager.CreateAsync(user, "qw123321");
            await _userManager.AddToRoleAsync(user, "admin");
        }

        // Seed test teacher user
        if (!_userManager.Users.AsNoTracking().Any(u => u.UserName.Equals("teacher_test")))
        {
            var user = new User
            {
                UserName = "teacher_test",
                Email = "teacher@test.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            await _userManager.CreateAsync(user, "Teacher@123");
            await _userManager.AddToRoleAsync(user, "teacher");
        }

        // Seed institution admin user
        if (!_userManager.Users.AsNoTracking().Any(u => u.UserName.Equals("inst_admin")))
        {
            var user = new User
            {
                UserName = "inst_admin",
                Email = "inst_admin@site.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            await _userManager.CreateAsync(user, "Admin@123");
            await _userManager.AddToRoleAsync(user, "InstitutionAdmin");
        }

        await SeedVegaAdminAsync();

    }

    private async Task SeedVegaAdminAsync()
    {
        var existingInstitution = await _dbContext.Institutions
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        if (existingInstitution is null)
            return;

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.UserName == "VegaAdmin");

        if (user is null)
        {
            user = new User
            {
                UserName = "VegaAdmin",
                Email = "vega.admin@site.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                InstitutionId = existingInstitution.Id,
            };

            var createResult = await _userManager.CreateAsync(user, "Vega1234");
            if (!createResult.Succeeded)
                return;
        }

        if (user.InstitutionId != existingInstitution.Id)
        {
            user.InstitutionId = existingInstitution.Id;
            await _userManager.UpdateAsync(user);
        }

        if (!await _userManager.IsInRoleAsync(user, "InstitutionAdmin"))
        {
            await _userManager.AddToRoleAsync(user, "InstitutionAdmin");
        }

        var membership = await _dbContext.InstitutionUsers
            .FirstOrDefaultAsync(x =>
                x.UserId == user.Id
                && x.InstitutionId == existingInstitution.Id);

        if (membership is null)
        {
            _dbContext.InstitutionUsers.Add(new InstitutionUser
            {
                InstitutionId = existingInstitution.Id,
                UserId = user.Id,
                AccessScope = "Admin access",
                IsPrimary = true,
                IsActive = true,
                JoinedAt = DateTime.UtcNow,
            });
        }
        else if (!membership.IsActive)
        {
            membership.IsActive = true;
            membership.LeftAt = null;
        }

        await _dbContext.SaveChangesAsync();

    }
}