using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

  // Challenges
  public async Task<List<Challenge>> GetClassroomChallengesAsync(int classroomId)
  {
    try
    {
      var classroom = await DbContext.Classrooms.AsNoTracking()
          .Where(c => c.Id == classroomId)
          .Select(c => new { c.Id, c.TeacherId })
          .FirstOrDefaultAsync();

      if (classroom is null)
      {
        return new List<Challenge>();
      }

      var directClassroomChallenges = await DbContext.Challenges.AsNoTracking()
          .Include(c => c.Game)
          .Include(c => c.GameTemplate)
          .Where(c => c.ClassroomId == classroomId && c.CreatedById == classroom.TeacherId)
          .ToListAsync();

      var legacyChallengeIds = await GetLegacyClassroomChallengeIdsAsync(classroomId);
      if (legacyChallengeIds.Count > 0)
      {
        var legacyChallenges = await DbContext.Challenges.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.GameTemplate)
            .Where(c => legacyChallengeIds.Contains(c.Id) && c.CreatedById == classroom.TeacherId)
            .ToListAsync();

        directClassroomChallenges.AddRange(legacyChallenges);
      }

      return directClassroomChallenges
          .GroupBy(c => c.Id)
          .Select(group => group.First())
          .OrderBy(c => c.OrderIndex)
          .ThenBy(c => c.DifficultyLevel)
          .ToList();
    }
    catch (PostgresException ex) when (ex.SqlState == "42703")
    {
      return await GetClassroomChallengesLegacySafeAsync(classroomId);
    }
  }

  private async Task<List<Challenge>> GetClassroomChallengesLegacySafeAsync(int classroomId)
  {
    var classroom = await DbContext.Classrooms.AsNoTracking()
        .Where(c => c.Id == classroomId)
        .Select(c => new { c.Id, c.TeacherId })
        .FirstOrDefaultAsync();

    if (classroom is null)
    {
      return new List<Challenge>();
    }

    var directClassroomChallenges = await DbContext.Challenges
        .FromSqlInterpolated($"""
          SELECT
            c.*,
            'Draft'::character varying(24) AS lifecycle_state,
            FALSE AS is_pinned,
            0::double precision AS recommended_score,
            NULL::timestamp with time zone AS last_activity_at
          FROM "Challenges" c
          WHERE c."ClassroomId" = {classroomId}
            AND c."CreatedById" = {classroom.TeacherId}
          """)
        .AsNoTracking()
        .Include(c => c.Game)
        .ToListAsync();

    var legacyChallengeIds = await GetLegacyClassroomChallengeIdsAsync(classroomId);
    if (legacyChallengeIds.Count > 0)
    {
      var ids = legacyChallengeIds.ToArray();
      var legacyChallenges = await DbContext.Challenges
          .FromSqlInterpolated($"""
            SELECT
              c.*,
              'Draft'::character varying(24) AS lifecycle_state,
              FALSE AS is_pinned,
              0::double precision AS recommended_score,
              NULL::timestamp with time zone AS last_activity_at
            FROM "Challenges" c
            WHERE c."CreatedById" = {classroom.TeacherId}
              AND c."Id" = ANY ({ids})
            """)
          .AsNoTracking()
          .Include(c => c.Game)
          .ToListAsync();

      directClassroomChallenges.AddRange(legacyChallenges);
    }

    return directClassroomChallenges
        .GroupBy(c => c.Id)
        .Select(group => group.First())
        .OrderBy(c => c.OrderIndex)
        .ThenBy(c => c.DifficultyLevel)
        .ToList();
  }

  private async Task<HashSet<int>> GetLegacyClassroomChallengeIdsAsync(int classroomId)
  {
    var challengeIds = new HashSet<int>();
    var connection = DbContext.Database.GetDbConnection();
    var shouldCloseConnection = connection.State != ConnectionState.Open;

    try
    {
      if (shouldCloseConnection)
      {
        await connection.OpenAsync();
      }

      await using var command = connection.CreateCommand();
      command.CommandText = "SELECT \"QuizId\" FROM \"ClassroomQuizzes\" WHERE \"ClassroomId\" = @classroomId";

      var classroomIdParameter = command.CreateParameter();
      classroomIdParameter.ParameterName = "@classroomId";
      classroomIdParameter.Value = classroomId;
      command.Parameters.Add(classroomIdParameter);

      await using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        if (reader.IsDBNull(0))
        {
          continue;
        }

        var rawQuizId = reader.GetString(0);
        if (int.TryParse(rawQuizId, out var challengeId))
        {
          challengeIds.Add(challengeId);
        }
      }
    }
    catch
    {
      // Legacy table might not exist in newer databases; ignore when unavailable.
    }
    finally
    {
      if (shouldCloseConnection && connection.State == ConnectionState.Open)
      {
        await connection.CloseAsync();
      }
    }

    return challengeIds;
  }
}
