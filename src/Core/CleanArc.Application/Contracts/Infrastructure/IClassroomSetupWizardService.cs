using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure;

public interface IClassroomSetupWizardService
{
  Task<OperationResult<SetupClassroomWizardResult>> SetupClassroomAsync(
      int teacherId,
      string className,
      string subject,
      int challengeId,
      string csvContent,
      CancellationToken cancellationToken);
}