using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class ClassroomRepository(ApplicationDbContext dbContext) : BaseAsyncRepository<Classroom>(dbContext), IClassroomRepository
{
  public async Task<List<Classroom>> GetStudentClassroomsAsync(int userId)
  {
    return await DbContext.ClassroomStudents.AsNoTracking()
        .Where(cs => cs.UserId == userId)
        .Include(cs => cs.Classroom)
            .ThenInclude(c => c.Teacher)
        .Select(cs => cs.Classroom)
        .ToListAsync();
  }

  public async Task<List<Classroom>> GetTeacherClassroomsAsync(int teacherId)
  {
    return await TableNoTracking
        .Include(c => c.Teacher)
        .Where(c => c.TeacherId == teacherId && c.IsActive)
        .ToListAsync();
  }

  public async Task<Classroom> GetClassroomByIdAsync(int classroomId)
  {
    return await TableNoTracking
        .Include(c => c.Teacher)
        .FirstOrDefaultAsync(c => c.Id == classroomId);
  }

  public async Task<Classroom> GetClassroomByJoinCodeAsync(string joinCode)
  {
    var normalizedJoinCode = joinCode?.Trim().ToUpperInvariant() ?? string.Empty;

    return await TableNoTracking
        .Include(c => c.Teacher)
      .FirstOrDefaultAsync(c => c.JoinCode == normalizedJoinCode && c.IsActive);
  }

  public async Task<Classroom> CreateClassroomAsync(Classroom classroom)
  {
    await AddAsync(classroom);
    await DbContext.SaveChangesAsync();
    return classroom;
  }

  public async Task UpdateClassroomAsync(Classroom classroom)
  {
    DbContext.Classrooms.Update(classroom);
    await DbContext.SaveChangesAsync();
  }

  public async Task DeleteClassroomAsync(int classroomId)
  {
    var classroom = await DbContext.Classrooms.FirstOrDefaultAsync(c => c.Id == classroomId);
    if (classroom != null)
    {
      classroom.IsActive = false;
      await DbContext.SaveChangesAsync();
    }
  }

  // Students
  public async Task<ClassroomStudent> JoinClassroomAsync(ClassroomStudent student)
  {
    DbContext.ClassroomStudents.Add(student);
    await DbContext.SaveChangesAsync();
    return student;
  }

  public async Task<ClassroomStudent> GetClassroomStudentAsync(int classroomId, int userId)
  {
    return await DbContext.ClassroomStudents.AsNoTracking()
        .FirstOrDefaultAsync(cs => cs.ClassroomId == classroomId && cs.UserId == userId);
  }

  public async Task<int> GetStudentCountAsync(int classroomId)
  {
    return await DbContext.ClassroomStudents.CountAsync(cs => cs.ClassroomId == classroomId);
  }

  public async Task<List<ClassroomStudent>> GetClassroomMembersAsync(int classroomId)
  {
    return await DbContext.ClassroomStudents.AsNoTracking()
        .Include(cs => cs.User)
        .Where(cs => cs.ClassroomId == classroomId)
        .OrderByDescending(cs => cs.User.Experience)
        .ToListAsync();
  }

  // Quizzes
  public async Task<List<ClassroomQuiz>> GetClassroomQuizzesAsync(int classroomId)
  {
    return await DbContext.ClassroomQuizzes.AsNoTracking()
        .Where(cq => cq.ClassroomId == classroomId)
        .OrderByDescending(cq => cq.AssignedDate)
        .ToListAsync();
  }

  public async Task<ClassroomQuiz> AssignQuizAsync(ClassroomQuiz quiz)
  {
    DbContext.ClassroomQuizzes.Add(quiz);
    await DbContext.SaveChangesAsync();
    return quiz;
  }

  public async Task<int> GetQuizCountAsync(int classroomId)
  {
    return await DbContext.ClassroomQuizzes.CountAsync(cq => cq.ClassroomId == classroomId);
  }

  // Leaderboard
  public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(string quizId, int? classroomId = null)
  {
    var query = DbContext.LeaderboardEntries.AsNoTracking()
        .Include(le => le.User)
        .Where(le => le.QuizId == quizId);

    if (classroomId.HasValue)
      query = query.Where(le => le.ClassroomId == classroomId.Value);

    return await query.OrderByDescending(le => le.Score)
        .ThenBy(le => le.TimeSpent)
        .ToListAsync();
  }

  public async Task<LeaderboardEntry> AddLeaderboardEntryAsync(LeaderboardEntry entry)
  {
    DbContext.LeaderboardEntries.Add(entry);
    await DbContext.SaveChangesAsync();
    return entry;
  }
}
