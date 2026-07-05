using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal sealed class GetClassroomAssignableStudentsQueryHandler(
    IUnitOfWork unitOfWork,
    IInstitutionUserReportRepository institutionUserReportRepository)
    : IRequestHandler<GetClassroomAssignableStudentsQuery, OperationResult<List<ClassroomAssignableStudentDto>>>
{
    public async ValueTask<OperationResult<List<ClassroomAssignableStudentDto>>> Handle(
        GetClassroomAssignableStudentsQuery request,
        CancellationToken cancellationToken)
    {
        var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
        if (classroom is null)
        {
            return OperationResult<List<ClassroomAssignableStudentDto>>.NotFoundResult("Classroom not found");
        }

        if (!request.IsAdmin && classroom.TeacherId != request.RequestingUserId)
        {
            return OperationResult<List<ClassroomAssignableStudentDto>>.UnauthorizedResult("You do not manage this classroom");
        }

        var institutionId = classroom.Teacher?.InstitutionId;
        if (!institutionId.HasValue)
        {
            return OperationResult<List<ClassroomAssignableStudentDto>>.ForbiddenResult("Unable to resolve the classroom institution.");
        }

        var members = await unitOfWork.ClassroomRepository.GetClassroomMembersAsync(request.ClassroomId);
        var memberIds = members.Select(member => member.UserId).ToHashSet();

        var rows = await institutionUserReportRepository.GetUsersAsync(
            new InstitutionUserReportFilter(
                InstitutionId: institutionId.Value,
                Role: "student",
                Tab: "all",
                Search: null),
            cancellationToken);

        static string BuildDisplayName(InstitutionUserReportRow row)
        {
            var name = string.Join(" ", new[] { row.FirstName, row.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)))
                .Trim();

            return string.IsNullOrWhiteSpace(name) ? row.UserName : name;
        }

        var students = rows
            .Where(row => row.IsActive && !memberIds.Contains(row.Id))
            .OrderBy(BuildDisplayName)
            .ThenBy(row => row.UserName)
            .Select(row => new ClassroomAssignableStudentDto(
                row.Id,
                BuildDisplayName(row),
                row.UserName,
                row.Email,
                row.ClassName,
                row.IsActive))
            .ToList();

        return OperationResult<List<ClassroomAssignableStudentDto>>.SuccessResult(students);
    }
}
