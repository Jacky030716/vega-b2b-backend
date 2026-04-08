namespace CleanArc.Application.Contracts.Infrastructure;

public interface IStudentImportService
{
  IReadOnlyList<ParsedStudentCredential> ParseStudents(string csvContent);
}