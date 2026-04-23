using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal sealed class InstitutionUserReportRepository(ApplicationDbContext dbContext) : IInstitutionUserReportRepository
{
    public async Task<IReadOnlyList<InstitutionUserReportRow>> GetUsersAsync(
        InstitutionUserReportFilter filter,
        CancellationToken cancellationToken)
    {
        var institutionId = filter.InstitutionId <= 0 ? 1 : filter.InstitutionId;
        var now = DateTime.UtcNow;

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.InstitutionId == institutionId || u.InstitutionId == null)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.FamilyName,
                u.UserName,
                u.Email,
                u.LockoutEnd
            })
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return [];
        }

        var userIds = users.Select(u => u.Id).ToArray();

        var roleMap = await dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(
                dbContext.Roles.AsNoTracking(),
                ur => ur.RoleId,
                role => role.Id,
                (ur, role) => new { ur.UserId, RoleName = role.Name ?? string.Empty })
            .ToListAsync(cancellationToken);

        var roleLookup = roleMap
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => NormalizeRole(x.RoleName)).FirstOrDefault(x => x is "student" or "teacher"));

        var validUserIds = userIds
            .Where(id => roleLookup.TryGetValue(id, out var role) && role is "student" or "teacher")
            .ToArray();

        if (validUserIds.Length == 0)
        {
            return [];
        }

        var teacherClassMap = await dbContext.Classrooms
            .AsNoTracking()
            .Where(c => validUserIds.Contains(c.TeacherId))
            .GroupBy(c => c.TeacherId)
            .Select(g => new
            {
                TeacherId = g.Key,
                ClassName = g.OrderBy(x => x.Name).Select(x => x.Name).FirstOrDefault(),
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.TeacherId, x => new { x.ClassName, x.Count }, cancellationToken);

        var studentClassMap = await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(cs => validUserIds.Contains(cs.UserId))
            .Join(
                dbContext.Classrooms.AsNoTracking(),
                cs => cs.ClassroomId,
                classroom => classroom.Id,
                (cs, classroom) => new { cs.UserId, cs.JoinedDate, classroom.Name })
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                ClassName = g.OrderBy(x => x.JoinedDate).Select(x => x.Name).FirstOrDefault(),
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.UserId, x => new { x.ClassName, x.Count }, cancellationToken);

        var refreshLogins = await dbContext.Set<UserRefreshToken>()
            .AsNoTracking()
            .Where(rt => validUserIds.Contains(rt.UserId))
            .GroupBy(rt => rt.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                LastLoginAt = g.Max(x => (DateTime?)x.CreatedAt)
            })
            .ToDictionaryAsync(x => x.UserId, x => x.LastLoginAt, cancellationToken);

        var studentLogins = await dbContext.StudentCredentials
            .AsNoTracking()
            .Where(sc => validUserIds.Contains(sc.UserId))
            .GroupBy(sc => sc.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                LastLoginAt = g.Max(x => x.LastSuccessfulLoginAt)
            })
            .ToDictionaryAsync(x => x.UserId, x => x.LastLoginAt, cancellationToken);

        var rows = new List<InstitutionUserReportRow>(validUserIds.Length);
        foreach (var user in users.Where(u => validUserIds.Contains(u.Id)))
        {
            var role = roleLookup[user.Id]!;

            teacherClassMap.TryGetValue(user.Id, out var teacherClass);
            studentClassMap.TryGetValue(user.Id, out var studentClass);

            var className = role == "teacher" ? teacherClass?.ClassName : studentClass?.ClassName;
            var isUnassigned = role == "teacher"
                ? (teacherClass?.Count ?? 0) == 0
                : (studentClass?.Count ?? 0) == 0;

            refreshLogins.TryGetValue(user.Id, out var refreshLoginAt);
            studentLogins.TryGetValue(user.Id, out var studentLoginAt);
            var lastLoginAt = MaxDate(refreshLoginAt, studentLoginAt);
            var hasLoggedIn = lastLoginAt.HasValue;
            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > now;
            var isActive = !isLocked;

            rows.Add(new InstitutionUserReportRow(
                Id: user.Id,
                FirstName: user.Name ?? string.Empty,
                LastName: user.FamilyName ?? string.Empty,
                UserName: user.UserName ?? string.Empty,
                Email: user.Email ?? string.Empty,
                Role: role,
                IsActive: isActive,
                LastLoginAt: lastLoginAt,
                ClassName: className,
                HasLoggedIn: hasLoggedIn,
                IsUnassigned: isUnassigned,
                CredentialHint: hasLoggedIn ? string.Empty : $"Username: {user.UserName}"));
        }

        var filtered = ApplyFilter(rows, filter);
        return filtered
            .OrderBy(x => x.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<InstitutionUserDetailsDto?> GetUserDetailsAsync(
        int institutionId,
        int userId,
        CancellationToken cancellationToken)
    {
        var normalizedInstitutionId = institutionId <= 0 ? 1 : institutionId;
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && (u.InstitutionId == normalizedInstitutionId || u.InstitutionId == null))
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.FamilyName,
                u.UserName,
                u.Email,
                u.LockoutEnd
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(
                dbContext.Roles.AsNoTracking(),
                ur => ur.RoleId,
                role => role.Id,
                (_, role) => role.Name ?? string.Empty)
            .ToListAsync(cancellationToken);

        var role = roles
            .Select(NormalizeRole)
            .FirstOrDefault(x => x is "student" or "teacher");

        if (role is not ("student" or "teacher"))
        {
            return null;
        }

        List<InstitutionUserClassroomDto> classrooms;
        if (role == "teacher")
        {
            classrooms = await dbContext.Classrooms
                .AsNoTracking()
                .Where(c => c.TeacherId == userId)
                .OrderBy(c => c.Name)
                .Select(c => new InstitutionUserClassroomDto(c.Id, c.Name))
                .ToListAsync(cancellationToken);
        }
        else
        {
            var studentClassrooms = await dbContext.ClassroomStudents
                .AsNoTracking()
                .Where(cs => cs.UserId == userId)
                .Join(
                    dbContext.Classrooms.AsNoTracking(),
                    cs => cs.ClassroomId,
                    classroom => classroom.Id,
                    (_, classroom) => new { classroom.Id, classroom.Name })
                .Distinct()
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            classrooms = studentClassrooms
                .Select(c => new InstitutionUserClassroomDto(c.Id, c.Name))
                .ToList();
        }

        var refreshLoginAt = await dbContext.Set<UserRefreshToken>()
            .AsNoTracking()
            .Where(rt => rt.UserId == userId)
            .MaxAsync(rt => (DateTime?)rt.CreatedAt, cancellationToken);

        var studentLoginAt = await dbContext.StudentCredentials
            .AsNoTracking()
            .Where(sc => sc.UserId == userId)
            .MaxAsync(sc => sc.LastSuccessfulLoginAt, cancellationToken);

        var lastLoginAt = MaxDate(refreshLoginAt, studentLoginAt);
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow;
        var isActive = !isLocked;

        var totalXp = await dbContext.UserProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (int?)p.TotalXP)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

        var totalStars = await dbContext.Attempts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .SumAsync(a => (int?)a.StarsEarned, cancellationToken) ?? 0;

        return new InstitutionUserDetailsDto(
            Id: user.Id,
            FirstName: user.Name ?? string.Empty,
            LastName: user.FamilyName ?? string.Empty,
            UserName: user.UserName ?? string.Empty,
            Email: user.Email ?? string.Empty,
            Role: role,
            IsActive: isActive,
            LastLoginAt: lastLoginAt,
            TotalXp: totalXp,
            TotalStars: totalStars,
            Classrooms: classrooms);
    }

    public async Task<bool> IsInstitutionTeacherOrStudentUserAsync(
        int institutionId,
        int userId,
        CancellationToken cancellationToken)
    {
        var normalizedInstitutionId = institutionId <= 0 ? 1 : institutionId;
        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.Id == userId && (u.InstitutionId == normalizedInstitutionId || u.InstitutionId == null),
                cancellationToken);

        if (!userExists)
        {
            return false;
        }

        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(
                dbContext.Roles.AsNoTracking(),
                ur => ur.RoleId,
                role => role.Id,
                (_, role) => role.Name ?? string.Empty)
            .ToListAsync(cancellationToken);

        return roles
            .Select(NormalizeRole)
            .Any(x => x is "student" or "teacher");
    }

    private static IEnumerable<InstitutionUserReportRow> ApplyFilter(
        IEnumerable<InstitutionUserReportRow> rows,
        InstitutionUserReportFilter filter)
    {
        var result = rows;
        var role = NormalizeFilter(filter.Role, "all");
        var tab = NormalizeFilter(filter.Tab, "all");
        var search = filter.Search?.Trim();

        if (role is "student" or "teacher")
        {
            result = result.Where(x => x.Role == role);
        }

        if (tab == "unassigned")
        {
            result = result.Where(x => x.IsUnassigned);
        }
        else if (tab == "inactive")
        {
            result = result.Where(x => !x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            result = result.Where(x =>
                ContainsIgnoreCase(x.UserName, search!) ||
                ContainsIgnoreCase(x.Email, search!) ||
                ContainsIgnoreCase(x.ClassName, search!));
        }

        return result;
    }

    private static string NormalizeRole(string roleName)
    {
        var normalized = roleName.Trim().ToLowerInvariant();
        return normalized switch
        {
            "institutionadmin" => "institutionadmin",
            "admin" => "institutionadmin",
            _ => normalized
        };
    }

    private static string NormalizeFilter(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().ToLowerInvariant();
    }

    private static bool ContainsIgnoreCase(string? source, string search)
    {
        if (string.IsNullOrEmpty(source))
        {
            return false;
        }

        return source.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime? MaxDate(DateTime? left, DateTime? right)
    {
        if (!left.HasValue) return right;
        if (!right.HasValue) return left;
        return left > right ? left : right;
    }
}
