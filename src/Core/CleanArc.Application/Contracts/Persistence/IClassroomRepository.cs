using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;

namespace CleanArc.Application.Contracts.Persistence;

public interface IClassroomRepository
{
  // Classrooms
  Task<List<Classroom>> GetStudentClassroomsAsync(int userId);
  Task<List<Classroom>> GetTeacherClassroomsAsync(int teacherId);
  Task<Classroom> GetClassroomByIdAsync(int classroomId);
  Task<Classroom> GetClassroomByJoinCodeAsync(string joinCode);
  Task<Classroom> CreateClassroomAsync(Classroom classroom);
  Task UpdateClassroomAsync(Classroom classroom);
  Task DeleteClassroomAsync(int classroomId);

  // Students
  Task<ClassroomStudent> JoinClassroomAsync(ClassroomStudent student);
  Task<ClassroomStudent> GetClassroomStudentAsync(int classroomId, int userId);
  Task<int> GetStudentCountAsync(int classroomId);
  Task<List<ClassroomStudent>> GetClassroomMembersAsync(int classroomId);

  // Challenges assigned to this classroom
  Task<List<Challenge>> GetClassroomChallengesAsync(int classroomId);
}

