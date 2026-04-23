namespace CleanArc.Application.Contracts.Persistence;

public record InstitutionUserReportFilter(
    int InstitutionId,
    string Role,
    string Tab,
    string? Search);

public record InstitutionUserReportRow(
    int Id,
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Role,
    bool IsActive,
    DateTime? LastLoginAt,
    string? ClassName,
    bool HasLoggedIn,
    bool IsUnassigned,
    string CredentialHint);

public record InstitutionUserClassroomDto(
    int Id,
    string Name);

public record InstitutionUserDetailsDto(
    int Id,
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Role,
    bool IsActive,
    DateTime? LastLoginAt,
    int TotalXp,
    int TotalStars,
    IReadOnlyList<InstitutionUserClassroomDto> Classrooms);

public interface IInstitutionUserReportRepository
{
    Task<IReadOnlyList<InstitutionUserReportRow>> GetUsersAsync(
        InstitutionUserReportFilter filter,
        CancellationToken cancellationToken);

    Task<InstitutionUserDetailsDto?> GetUserDetailsAsync(
        int institutionId,
        int userId,
        CancellationToken cancellationToken);

    Task<bool> IsInstitutionTeacherOrStudentUserAsync(
        int institutionId,
        int userId,
        CancellationToken cancellationToken);
}
