namespace CleanArc.Application.Contracts.Infrastructure;

public interface IRosterPdfGenerator
{
  Task<byte[]> GenerateRosterPdfAsync(
      string classroomName,
      IReadOnlyList<StudentCredentialPreview> students,
      CancellationToken cancellationToken);
}