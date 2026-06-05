using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;

namespace CleanArc.Application.Contracts.Persistence;

public interface IClassroomRepository
{
  // Classrooms
  Task<List<Classroom>> GetStudentClassroomsAsync(int userId);
  Task<List<Classroom>> GetTeacherClassroomsAsync(int teacherId, bool includeDeleted = false);
  Task<Classroom> GetClassroomByIdAsync(int classroomId, bool includeDeleted = false, bool tracking = false);
  Task<Classroom> GetClassroomByJoinCodeAsync(string joinCode);
  Task<Classroom> CreateClassroomAsync(Classroom classroom);
  Task UpdateClassroomAsync(Classroom classroom);
  Task ProvisionClassroomModulesAsync(int classroomId, IEnumerable<string> subjects, int teacherId);
  Task ReplaceClassroomSubjectsAndModulesAsync(int classroomId, IEnumerable<string> subjects, int teacherId);
  Task<IReadOnlyList<string>> GetClassroomSubjectsAsync(int classroomId);
  Task<bool> IsModuleAttachedToClassroomAsync(int classroomId, int moduleId);
  Task<int?> ResolveChallengeModuleIdAsync(int classroomId);
  Task ArchiveClassroomAsync(Classroom classroom, int deletedBy);
  Task DeleteClassroomAsync(int classroomId);
  Task<bool> HasModulesOrChallengesAsync(int classroomId);
  Task<int> GetInstitutionClassroomsCountAsync(int institutionId, CancellationToken cancellationToken = default);


  // Students
  Task<ClassroomStudent> JoinClassroomAsync(ClassroomStudent student);
  Task<ClassroomStudent> GetClassroomStudentAsync(int classroomId, int userId);
  Task<int> GetStudentCountAsync(int classroomId);
  Task<List<ClassroomStudent>> GetClassroomMembersAsync(int classroomId);
  Task<IReadOnlyDictionary<int, int>> GetClassroomStudentStarsAsync(int classroomId);

  // Challenges assigned to this classroom
  Task<List<Challenge>> GetClassroomChallengesAsync(int classroomId);

  Task<int> GetModuleCountAsync(int classroomId);
}
