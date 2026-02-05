using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Identity.Identity.Manager;
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

    public SeedDataBase(AppUserManager userManager, AppRoleManager roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
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
    }
}