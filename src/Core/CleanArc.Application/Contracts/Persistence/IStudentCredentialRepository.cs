using CleanArc.Domain.Entities.User;

namespace CleanArc.Application.Contracts.Persistence;

public interface IStudentCredentialRepository
{
  Task<StudentCredential?> GetByLoginCodeAsync(string loginCode);
  Task<List<StudentCredential>> GetByClassroomIdAsync(int classroomId);
  Task<StudentCredential> CreateAsync(StudentCredential credential);
  Task UpdateAsync(StudentCredential credential);
}
