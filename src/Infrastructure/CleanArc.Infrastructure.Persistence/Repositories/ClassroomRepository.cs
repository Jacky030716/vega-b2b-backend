using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.Adaptive;
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
        .Where(cs => cs.UserId == userId && cs.Classroom.IsActive && !cs.Classroom.IsDeleted)
        .Include(cs => cs.Classroom)
            .ThenInclude(c => c.Subjects)
        .Include(cs => cs.Classroom)
            .ThenInclude(c => c.Teacher)
        .Select(cs => cs.Classroom)
        .ToListAsync();
  }

  public async Task<List<Classroom>> GetTeacherClassroomsAsync(int teacherId, bool includeDeleted = false)
  {
    var query = TableNoTracking
        .Include(c => c.Teacher)
        .Include(c => c.Subjects)
        .Where(c => c.TeacherId == teacherId);

    if (!includeDeleted)
    {
      query = query.Where(c => c.IsActive && !c.IsDeleted);
    }

    return await query
        .ToListAsync();
  }

  public async Task<Classroom> GetClassroomByIdAsync(int classroomId, bool includeDeleted = false, bool tracking = false)
  {
    var query = tracking ? DbContext.Classrooms.AsQueryable() : TableNoTracking;
    if (!includeDeleted)
    {
      query = query.Where(c => c.IsActive && !c.IsDeleted);
    }

    return await query
      .Include(c => c.Teacher)
      .Include(c => c.Subjects)
      .FirstOrDefaultAsync(c => c.Id == classroomId);
  }

  public async Task<Classroom> GetClassroomByJoinCodeAsync(string joinCode)
  {
    var normalizedJoinCode = joinCode?.Trim().ToUpperInvariant() ?? string.Empty;

    return await TableNoTracking
        .Include(c => c.Teacher)
      .FirstOrDefaultAsync(c => c.JoinCode == normalizedJoinCode && c.IsActive && !c.IsDeleted);
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

  public async Task ProvisionClassroomModulesAsync(int classroomId, IEnumerable<string> subjects, int teacherId)
  {
    var classroom = await DbContext.Classrooms
        .Include(c => c.Subjects)
        .FirstOrDefaultAsync(c => c.Id == classroomId && c.IsActive && !c.IsDeleted)
        ?? throw new InvalidOperationException("Classroom not found");

    if (classroom.TeacherId != teacherId)
    {
      throw new UnauthorizedAccessException("You do not manage this classroom");
    }

    var normalizedSubjects = NormalizeSubjects(subjects);
    if (normalizedSubjects.Count == 0 && !string.IsNullOrWhiteSpace(classroom.Subject))
    {
      normalizedSubjects.Add(classroom.Subject.Trim());
    }

    if (normalizedSubjects.Count == 0)
    {
      return;
    }

    classroom.Subject = normalizedSubjects[0];

    var existingSubjects = classroom.Subjects
        .Select(s => s.Subject)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (var subject in normalizedSubjects)
    {
      if (existingSubjects.Add(subject))
      {
        DbContext.ClassroomSubjects.Add(new ClassroomSubject
        {
          ClassroomId = classroom.Id,
          Subject = subject
        });
      }
    }

    var matchingModuleIds = await DbContext.SyllabusModules.AsNoTracking()
        .Where(m => m.IsActive
                    && m.ModuleType == SyllabusModule.PredefinedModuleType
                    && m.YearLevel == classroom.YearLevel
                    && normalizedSubjects.Contains(m.Subject))
        .Select(m => m.Id)
        .ToListAsync();

    if (matchingModuleIds.Count > 0)
    {
      var existingModuleIds = await DbContext.ClassroomModules.AsNoTracking()
          .Where(m => m.ClassroomId == classroom.Id && matchingModuleIds.Contains(m.ModuleId))
          .Select(m => m.ModuleId)
          .ToListAsync();

      var existingSet = existingModuleIds.ToHashSet();
      foreach (var moduleId in matchingModuleIds.Where(id => !existingSet.Contains(id)))
      {
        DbContext.ClassroomModules.Add(new ClassroomModule
        {
          ClassroomId = classroom.Id,
          ModuleId = moduleId
        });
      }
    }

    var hasCustomModule = await DbContext.ClassroomModules.AsNoTracking()
        .Include(m => m.Module)
        .AnyAsync(m => m.ClassroomId == classroom.Id && m.Module.ModuleType == SyllabusModule.CustomModuleType);
    if (!hasCustomModule)
    {
      DbContext.ClassroomModules.Add(new ClassroomModule
      {
        ClassroomId = classroom.Id,
        Module = CreateCustomLearningModule(classroom, teacherId)
      });
    }

    await DbContext.SaveChangesAsync();
  }

  public async Task<IReadOnlyList<string>> GetClassroomSubjectsAsync(int classroomId)
  {
    var subjects = await DbContext.ClassroomSubjects.AsNoTracking()
        .Where(s => s.ClassroomId == classroomId)
        .OrderBy(s => s.Subject)
        .Select(s => s.Subject)
        .ToListAsync();

    if (subjects.Count > 0)
    {
      return subjects;
    }

    var legacySubject = await DbContext.Classrooms.AsNoTracking()
        .Where(c => c.Id == classroomId && c.IsActive && !c.IsDeleted)
        .Select(c => c.Subject)
        .FirstOrDefaultAsync();

    return string.IsNullOrWhiteSpace(legacySubject)
        ? Array.Empty<string>()
        : new[] { legacySubject.Trim() };
  }

  public async Task ReplaceClassroomSubjectsAndModulesAsync(int classroomId, IEnumerable<string> subjects, int teacherId)
  {
    var classroom = await DbContext.Classrooms
        .Include(c => c.Subjects)
        .FirstOrDefaultAsync(c => c.Id == classroomId && c.IsActive && !c.IsDeleted)
        ?? throw new InvalidOperationException("Classroom not found");

    if (classroom.TeacherId != teacherId)
    {
      throw new UnauthorizedAccessException("You do not manage this classroom");
    }

    var normalizedSubjects = NormalizeSubjects(subjects);
    if (normalizedSubjects.Count == 0)
    {
      throw new InvalidOperationException("At least one subject is required");
    }

    classroom.Subject = normalizedSubjects[0];

    var subjectSet = normalizedSubjects.ToHashSet(StringComparer.OrdinalIgnoreCase);
    var subjectsToRemove = classroom.Subjects
        .Where(subject => !subjectSet.Contains(subject.Subject))
        .ToList();
    DbContext.ClassroomSubjects.RemoveRange(subjectsToRemove);

    var existingSubjects = classroom.Subjects
        .Where(subject => !subjectsToRemove.Contains(subject))
        .Select(subject => subject.Subject)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (var subject in normalizedSubjects.Where(subject => !existingSubjects.Contains(subject)))
    {
      DbContext.ClassroomSubjects.Add(new ClassroomSubject
      {
        ClassroomId = classroom.Id,
        Subject = subject
      });
    }

    var desiredModuleIds = await DbContext.SyllabusModules.AsNoTracking()
        .Where(module => module.IsActive
                         && module.ModuleType == SyllabusModule.PredefinedModuleType
                         && module.YearLevel == classroom.YearLevel
                         && normalizedSubjects.Contains(module.Subject))
        .Select(module => module.Id)
        .ToListAsync();
    var desiredSet = desiredModuleIds.ToHashSet();

    var existingLinks = await DbContext.ClassroomModules
        .Where(link => link.ClassroomId == classroom.Id)
        .ToListAsync();
    var customModuleIds = await DbContext.SyllabusModules.AsNoTracking()
        .Where(module => module.ModuleType == SyllabusModule.CustomModuleType)
        .Select(module => module.Id)
        .ToListAsync();
    var customSet = customModuleIds.ToHashSet();
    DbContext.ClassroomModules.RemoveRange(existingLinks.Where(link => !desiredSet.Contains(link.ModuleId) && !customSet.Contains(link.ModuleId)));

    var existingModuleIds = existingLinks.Select(link => link.ModuleId).ToHashSet();
    foreach (var moduleId in desiredModuleIds.Where(moduleId => !existingModuleIds.Contains(moduleId)))
    {
      DbContext.ClassroomModules.Add(new ClassroomModule
      {
        ClassroomId = classroom.Id,
        ModuleId = moduleId
      });
    }

    var hasCustomModule = await DbContext.ClassroomModules.AsNoTracking()
        .Include(module => module.Module)
        .AnyAsync(module => module.ClassroomId == classroom.Id && module.Module.ModuleType == SyllabusModule.CustomModuleType);
    if (!hasCustomModule)
    {
      DbContext.ClassroomModules.Add(new ClassroomModule
      {
        ClassroomId = classroom.Id,
        Module = CreateCustomLearningModule(classroom, teacherId)
      });
    }

    await DbContext.SaveChangesAsync();
  }

  public async Task<bool> IsModuleAttachedToClassroomAsync(int classroomId, int moduleId)
  {
    return await DbContext.ClassroomModules.AsNoTracking()
        .AnyAsync(m => m.ClassroomId == classroomId && m.ModuleId == moduleId);
  }

  public async Task<int?> ResolveChallengeModuleIdAsync(int classroomId)
  {
    var modules = await DbContext.ClassroomModules.AsNoTracking()
        .Include(link => link.Module)
        .Where(link => link.ClassroomId == classroomId && link.Module.IsActive)
        .Select(link => new
        {
          link.ModuleId,
          link.Module.ModuleType
        })
        .ToListAsync();

    var predefinedModules = modules
        .Where(module => module.ModuleType == SyllabusModule.PredefinedModuleType)
        .ToList();
    if (predefinedModules.Count == 1)
    {
      return predefinedModules[0].ModuleId;
    }

    return modules
        .FirstOrDefault(module => module.ModuleType == SyllabusModule.CustomModuleType)
        ?.ModuleId;
  }

  public async Task DeleteClassroomAsync(int classroomId)
  {
    var classroom = await DbContext.Classrooms.FirstOrDefaultAsync(c => c.Id == classroomId);
    if (classroom != null)
    {
      classroom.IsActive = false;
      classroom.IsDeleted = true;
      classroom.DeletedAt = DateTime.UtcNow;
      await DbContext.SaveChangesAsync();
    }
  }

  public async Task ArchiveClassroomAsync(Classroom classroom, int deletedBy)
  {
    classroom.IsActive = false;
    classroom.IsDeleted = true;
    classroom.DeletedAt = DateTime.UtcNow;
    classroom.DeletedBy = deletedBy;
    await DbContext.SaveChangesAsync();
  }

  public async Task<bool> HasModulesOrChallengesAsync(int classroomId)
  {
    var hasChallenges = await DbContext.Challenges.AsNoTracking()
        .AnyAsync(c => c.ClassroomId == classroomId);
    if (hasChallenges)
    {
      return true;
    }

    return await DbContext.ClassroomModules.AsNoTracking()
        .AnyAsync(link => link.ClassroomId == classroomId);
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

  public async Task<int> GetModuleCountAsync(int classroomId)
  {
    return await DbContext.ClassroomModules.CountAsync(cm => cm.ClassroomId == classroomId);
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
          .Where(c => c.Id == classroomId && c.IsActive && !c.IsDeleted)
          .Select(c => new { c.Id, c.TeacherId })
          .FirstOrDefaultAsync();

      if (classroom is null)
      {
        return new List<Challenge>();
      }

      var directClassroomChallenges = await DbContext.Challenges.AsNoTracking()
          .Include(c => c.Game)
          .Include(c => c.GameTemplate)
          .Where(c => c.ClassroomId == classroomId && c.CreatedById == classroom.TeacherId && c.SourceType != "RECOVERY_MISSION")
          .ToListAsync();

      var legacyChallengeIds = await GetLegacyClassroomChallengeIdsAsync(classroomId);
      if (legacyChallengeIds.Count > 0)
      {
        var legacyChallenges = await DbContext.Challenges.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.GameTemplate)
            .Where(c => legacyChallengeIds.Contains(c.Id) && c.CreatedById == classroom.TeacherId && c.SourceType != "RECOVERY_MISSION")
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
        .Where(c => c.Id == classroomId && c.IsActive && !c.IsDeleted)
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

  private static List<string> NormalizeSubjects(IEnumerable<string> subjects)
  {
    return subjects
        .Where(subject => !string.IsNullOrWhiteSpace(subject))
        .Select(subject => subject.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
  }

  private static SyllabusModule CreateCustomLearningModule(Classroom classroom, int teacherId) => new()
  {
    ModuleCode = $"CUSTOM-{classroom.Id}-{Guid.NewGuid():N}",
    Subject = string.IsNullOrWhiteSpace(classroom.Subject) ? "Custom" : classroom.Subject.Trim(),
    Language = "ms",
    YearLevel = classroom.YearLevel,
    Term = string.Empty,
    UnitTitle = "Custom Module",
    Title = "Custom Module",
    Description = "Teacher-created learning module.",
    ModuleType = SyllabusModule.CustomModuleType,
    SourceType = "teacher_created",
    CreatedByTeacherId = teacherId
  };
}
