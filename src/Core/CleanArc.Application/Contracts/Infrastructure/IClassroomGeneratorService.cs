using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Contracts.Infrastructure;

public interface IClassroomGeneratorService
{
    Task<OperationResult<byte[]>> BulkCreateClassroomAsync(string name, string description, string subject, string csvContent, int teacherId, CancellationToken cancellationToken);
}
