using CleanArc.Application.Contracts.Identity;
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
        // Seed student role
        if (!_roleManager.Roles.AsNoTracking().Any(r => r.Name.ToLower() == RoleNames.Student.ToLower()))
        {
            var role = new Role
            {
                Name = RoleNames.Student,
                DisplayName = "Student",
                CreatedDate = DateTime.UtcNow
            };
            await _roleManager.CreateAsync(role);
        }

        // Seed teacher role
        if (!_roleManager.Roles.AsNoTracking().Any(r => r.Name.ToLower() == RoleNames.Teacher.ToLower()))
        {
            var role = new Role
            {
                Name = RoleNames.Teacher,
                DisplayName = "Teacher",
                CreatedDate = DateTime.UtcNow
            };
            await _roleManager.CreateAsync(role);
        }

        // Seed institution admin role
        if (!_roleManager.Roles.AsNoTracking().Any(r => r.Name.ToLower() == RoleNames.InstitutionAdmin.ToLower()))
        {
            var role = new Role
            {
                Name = RoleNames.InstitutionAdmin,
                DisplayName = "Institution Admin",
                CreatedDate = DateTime.UtcNow
            };
            await _roleManager.CreateAsync(role);
        }
        await SeedVegaAdminAsync();

    }

    private async Task SeedVegaAdminAsync()
    {
        var existingInstitution = await _dbContext.Institutions
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        if (existingInstitution is null)
        {
            existingInstitution = new Institution
            {
                Name = "Vega Academy",
                MaxSeats = 1000,
                SeatsUsed = 0,
                RenewalDate = DateTime.UtcNow.AddYears(1),
                SubscriptionTier = "premium",
                StripeCustomerId = string.Empty
            };
            _dbContext.Institutions.Add(existingInstitution);
            await _dbContext.SaveChangesAsync();
        }

        var billingAccount = await _dbContext.BillingAccounts
            .FirstOrDefaultAsync(x => x.InstitutionId == existingInstitution.Id);

        if (billingAccount is null)
        {
            billingAccount = new CleanArc.Domain.Entities.Billing.BillingAccount
            {
                InstitutionId = existingInstitution.Id,
                PlanId = "premium-annual",
                ActivePlanId = "premium-annual",
                Status = "DEMO_SUCCEEDED",
                StripeCustomerId = string.Empty
            };
            _dbContext.BillingAccounts.Add(billingAccount);

            var transaction = new CleanArc.Domain.Entities.Billing.PaymentTransaction
            {
                InstitutionId = existingInstitution.Id,
                Provider = "demo-wallet",
                PaymentMethod = "touch-n-go",
                PlanId = "premium-annual",
                Amount = 29900m,
                Currency = "MYR",
                Status = "DEMO_SUCCEEDED",
                IsDemo = true,
                CreatedTime = DateTime.UtcNow.AddDays(-37)
            };
            _dbContext.PaymentTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync();
        }

        // Link existing default users (admin, teacher_test, inst_admin) to this institution if they exist
        var adminUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
        if (adminUser is not null)
        {
            if (!adminUser.InstitutionId.HasValue || adminUser.InstitutionId != existingInstitution.Id)
            {
                adminUser.InstitutionId = existingInstitution.Id;
                await _userManager.UpdateAsync(adminUser);
            }

            if (!await _userManager.IsInRoleAsync(adminUser, RoleNames.InstitutionAdmin))
            {
                await _userManager.AddToRoleAsync(adminUser, RoleNames.InstitutionAdmin);
            }

            var exists = await _dbContext.InstitutionUsers.AnyAsync(x => x.UserId == adminUser.Id && x.InstitutionId == existingInstitution.Id);
            if (!exists)
            {
                _dbContext.InstitutionUsers.Add(new InstitutionUser
                {
                    InstitutionId = existingInstitution.Id,
                    UserId = adminUser.Id,
                    AccessScope = "Admin access",
                    IsPrimary = true,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        var teacherTestUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == "teacher_test");
        if (teacherTestUser is not null && (!teacherTestUser.InstitutionId.HasValue || teacherTestUser.InstitutionId != existingInstitution.Id))
        {
            teacherTestUser.InstitutionId = existingInstitution.Id;
            await _userManager.UpdateAsync(teacherTestUser);

            var exists = await _dbContext.InstitutionUsers.AnyAsync(x => x.UserId == teacherTestUser.Id && x.InstitutionId == existingInstitution.Id);
            if (!exists)
            {
                _dbContext.InstitutionUsers.Add(new InstitutionUser
                {
                    InstitutionId = existingInstitution.Id,
                    UserId = teacherTestUser.Id,
                    AccessScope = "Teacher access",
                    IsPrimary = true,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        var instAdminUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == "inst_admin");
        if (instAdminUser is not null && (!instAdminUser.InstitutionId.HasValue || instAdminUser.InstitutionId != existingInstitution.Id))
        {
            instAdminUser.InstitutionId = existingInstitution.Id;
            await _userManager.UpdateAsync(instAdminUser);

            var exists = await _dbContext.InstitutionUsers.AnyAsync(x => x.UserId == instAdminUser.Id && x.InstitutionId == existingInstitution.Id);
            if (!exists)
            {
                _dbContext.InstitutionUsers.Add(new InstitutionUser
                {
                    InstitutionId = existingInstitution.Id,
                    UserId = instAdminUser.Id,
                    AccessScope = "Admin access",
                    IsPrimary = true,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        await _dbContext.SaveChangesAsync();

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

        if (!await _userManager.IsInRoleAsync(user, RoleNames.InstitutionAdmin))
        {
            await _userManager.AddToRoleAsync(user, RoleNames.InstitutionAdmin);
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

        // Link existing students in classrooms to their teacher's institution
        var classroomStudents = await _dbContext.ClassroomStudents
            .Include(cs => cs.Classroom)
            .ToListAsync();

        foreach (var cs in classroomStudents)
        {
            var teacher = await _userManager.FindByIdAsync(cs.Classroom.TeacherId.ToString());
            if (teacher is not null && teacher.InstitutionId.HasValue)
            {
                var studentUser = await _userManager.FindByIdAsync(cs.UserId.ToString());
                if (studentUser is not null && (!studentUser.InstitutionId.HasValue || studentUser.InstitutionId != teacher.InstitutionId))
                {
                    studentUser.InstitutionId = teacher.InstitutionId;
                    await _userManager.UpdateAsync(studentUser);
                }

                var exists = await _dbContext.InstitutionUsers.AnyAsync(x => x.UserId == cs.UserId && x.InstitutionId == teacher.InstitutionId.Value);
                if (!exists)
                {
                    _dbContext.InstitutionUsers.Add(new InstitutionUser
                    {
                        InstitutionId = teacher.InstitutionId.Value,
                        UserId = cs.UserId,
                        AccessScope = "Student access",
                        IsPrimary = true,
                        IsActive = true,
                        JoinedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _dbContext.SaveChangesAsync();

    }
}
