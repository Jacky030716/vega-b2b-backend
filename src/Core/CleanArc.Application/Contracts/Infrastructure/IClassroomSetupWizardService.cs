using System.Collections.Generic;
using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure;

public interface IClassroomSetupWizardService
{
  Task<OperationResult<SetupClassroomWizardResult>> SetupClassroomAsync(
      int teacherId,
      string className,
      string subject,
      string gameKey,
      string csvContent,
      int yearLevel,
      IReadOnlyList<string>? subjects,
      CancellationToken cancellationToken);
}