using CleanArc.Application.Contracts.Infrastructure;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands.SetupClassroom;

internal class SetupClassroomCommandHandler(
    IClassroomSetupWizardService classroomSetupWizardService)
    : IRequestHandler<SetupClassroomCommand, OperationResult<SetupClassroomWizardResult>>
{
  public async ValueTask<OperationResult<SetupClassroomWizardResult>> Handle(
      SetupClassroomCommand request,
      CancellationToken cancellationToken)
  {
    return await classroomSetupWizardService.SetupClassroomAsync(
        request.TeacherId,
        request.ClassName,
        request.Subject,
        request.ChallengeId,
        request.CsvContent,
        cancellationToken);
  }
}