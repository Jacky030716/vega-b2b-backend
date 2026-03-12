using CleanArc.Domain.Entities.Classroom;

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

  // Quizzes
  Task<List<ClassroomQuiz>> GetClassroomQuizzesAsync(int classroomId);
  Task<ClassroomQuiz> AssignQuizAsync(ClassroomQuiz quiz);
  Task<int> GetQuizCountAsync(int classroomId);

  // Leaderboard
  Task<List<LeaderboardEntry>> GetLeaderboardAsync(string quizId, int? classroomId = null);
  Task<LeaderboardEntry> AddLeaderboardEntryAsync(LeaderboardEntry entry);
}
