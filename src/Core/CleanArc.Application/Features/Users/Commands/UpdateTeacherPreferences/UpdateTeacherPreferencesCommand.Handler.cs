using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Users.Commands.UpdateTeacherPreferences;

internal class UpdateTeacherPreferencesCommandHandler(
    IAppUserManager userManager)
    : IRequestHandler<UpdateTeacherPreferencesCommand, OperationResult<UpdateTeacherPreferencesResult>>
{
  public async ValueTask<OperationResult<UpdateTeacherPreferencesResult>> Handle(
      UpdateTeacherPreferencesCommand request,
      CancellationToken cancellationToken)
  {
    var teacher = await userManager.GetUserByIdAsync(request.TeacherId);
    if (teacher is null)
      return OperationResult<UpdateTeacherPreferencesResult>.NotFoundResult("Teacher not found");

    teacher.WeeklyAiInsightsEmail = request.WeeklyAiInsightsEmail;
    teacher.InactiveStudentAlerts = request.InactiveStudentAlerts;

    var updateResult = await userManager.UpdateUser(teacher);
    if (!updateResult.Succeeded)
    {
      var errorMessage = updateResult.Errors.FirstOrDefault()?.Description
          ?? "Unable to update teacher preferences";
      return OperationResult<UpdateTeacherPreferencesResult>.FailureResult(errorMessage);
    }

    return OperationResult<UpdateTeacherPreferencesResult>.SuccessResult(
        new UpdateTeacherPreferencesResult(
            teacher.WeeklyAiInsightsEmail,
            teacher.InactiveStudentAlerts));
  }
}
