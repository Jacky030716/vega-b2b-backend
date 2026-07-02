namespace CleanArc.Application.Contracts.Identity;

public static class RoleNames
{
    public const string Student = "student";
    public const string Teacher = "teacher";
    public const string InstitutionAdmin = "institution_admin";

    public static bool IsAdmin(string? roleName)
    {
        return string.Equals(roleName, "admin", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInstitutionAdmin(string? roleName)
    {
        if (string.IsNullOrEmpty(roleName))
            return false;

        var normalized = roleName.Replace("_", "").ToLowerInvariant();
        return normalized == "institutionadmin" || normalized == "admin";
    }
}
