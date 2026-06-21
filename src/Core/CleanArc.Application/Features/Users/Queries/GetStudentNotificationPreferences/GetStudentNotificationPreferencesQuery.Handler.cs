using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Features.Users.StudentNotificationPreferences;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.GetStudentNotificationPreferences;

internal sealed class GetStudentNotificationPreferencesQueryHandler(
    IAppUserManager userManager)
    : IRequestHandler<GetStudentNotificationPreferencesQuery, OperationResult<StudentNotificationPreferencesDto>>
{
    public async ValueTask<OperationResult<StudentNotificationPreferencesDto>> Handle(
        GetStudentNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var student = await userManager.GetUserByIdAsync(request.StudentId);
        if (student is null)
            return OperationResult<StudentNotificationPreferencesDto>.NotFoundResult("Student not found");

        return OperationResult<StudentNotificationPreferencesDto>.SuccessResult(
            StudentNotificationPreferencesMapper.FromUser(student));
    }
}
