using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using BCrypt.Net;
using CleanArc.Application.Contracts.Adaptive;
using CleanArc.Application.Contracts.Infrastructure;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services;

public class ClassroomSetupWizardService(
    ApplicationDbContext dbContext,
    UserManager<User> userManager,
    IStudentImportService studentImportService,
    IRosterPdfGenerator rosterPdfGenerator,
    IChallengeOrchestrator challengeOrchestrator)
    : IClassroomSetupWizardService
{
  private static readonly Regex UnsafeUserNameChars = new("[^a-zA-Z0-9]", RegexOptions.Compiled);

  public async Task<OperationResult<SetupClassroomWizardResult>> SetupClassroomAsync(
      int teacherId,
      string className,
      string subject,
      string gameKey,
      string csvContent,
      int yearLevel,
      IReadOnlyList<string>? subjects,
      CancellationToken cancellationToken)
  {
    var parsedStudents = studentImportService.ParseStudents(csvContent);
    if (parsedStudents.Count == 0)
      return OperationResult<SetupClassroomWizardResult>.FailureResult("CSV did not contain any valid student names");

    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    try
    {
      var normalizedSubjects = (subjects ?? new List<string>())
          .Where(s => !string.IsNullOrWhiteSpace(s))
          .Select(s => s.Trim())
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .ToList();

      if (normalizedSubjects.Count == 0 && !string.IsNullOrWhiteSpace(subject))
      {
        normalizedSubjects.Add(subject.Trim());
      }

      if (normalizedSubjects.Count == 0)
      {
        normalizedSubjects.Add("General");
      }

      var classroom = new Classroom
      {
        Name = className.Trim(),
        Description = "Created with Classroom Setup Wizard",
        Subject = normalizedSubjects[0],
        YearLevel = yearLevel,
        Thumbnail = string.Empty,
        JoinCode = await GenerateUniqueJoinCodeAsync(cancellationToken),
        TeacherId = teacherId,
        IsActive = true
      };

      dbContext.Classrooms.Add(classroom);
      await dbContext.SaveChangesAsync(cancellationToken);

      foreach (var sub in normalizedSubjects)
      {
        dbContext.ClassroomSubjects.Add(new ClassroomSubject
        {
          ClassroomId = classroom.Id,
          Subject = sub
        });
      }
      await dbContext.SaveChangesAsync(cancellationToken);

      var matchingModuleIds = await dbContext.SyllabusModules.AsNoTracking()
          .Where(m => m.IsActive
                      && m.ModuleType == SyllabusModule.PredefinedModuleType
                      && m.YearLevel == classroom.YearLevel
                      && normalizedSubjects.Contains(m.Subject))
          .Select(m => m.Id)
          .ToListAsync(cancellationToken);

      foreach (var moduleId in matchingModuleIds)
      {
        dbContext.ClassroomModules.Add(new ClassroomModule
        {
          ClassroomId = classroom.Id,
          ModuleId = moduleId
        });
      }

      var customModule = new SyllabusModule
      {
        ModuleCode = $"CUSTOM-{classroom.Id}-{Guid.NewGuid():N}",
        Subject = classroom.Subject,
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
      dbContext.SyllabusModules.Add(customModule);
      dbContext.ClassroomModules.Add(new ClassroomModule
      {
        ClassroomId = classroom.Id,
        Module = customModule
      });
      await dbContext.SaveChangesAsync(cancellationToken);

      var existingLoginCodeList = await dbContext.StudentCredentials
        .AsNoTracking()
        .Select(sc => sc.StudentLoginCode)
        .ToListAsync(cancellationToken);

      var existingLoginCodes = new HashSet<string>(existingLoginCodeList, StringComparer.OrdinalIgnoreCase);

      var rosterPreview = new List<StudentCredentialPreview>();

      foreach (var student in parsedStudents)
      {
        var loginCode = EnsureUniqueLoginCode(student.StudentLoginCode, existingLoginCodes);
        var user = await CreateStudentUserAsync(student.StudentName, cancellationToken);

        dbContext.ClassroomStudents.Add(new ClassroomStudent
        {
          ClassroomId = classroom.Id,
          UserId = user.Id,
          JoinedDate = DateTime.UtcNow
        });

        dbContext.StudentCredentials.Add(new StudentCredential
        {
          UserId = user.Id,
          ClassroomId = classroom.Id,
          StudentLoginCode = loginCode,
          VisualPasswordHash = CleanArc.Application.Common.VisualPasswordHelper.HashPassword(student.VisualPassword, loginCode),
          IsActive = true,
          FailedAttempts = 0
        });

        rosterPreview.Add(new StudentCredentialPreview(student.StudentName, loginCode, student.VisualPassword));
      }
      await dbContext.SaveChangesAsync(cancellationToken);

      var targetModuleId = matchingModuleIds.Count > 0 ? matchingModuleIds[0] : customModule.Id;

      var vocabularyItems = await dbContext.VocabularyItems.AsNoTracking()
          .Where(v => v.ModuleId == targetModuleId && v.IsActive)
          .ToListAsync(cancellationToken);

      var fallbackWords = new List<string> { "buku", "mata", "sekolah", "makan" };
      if (classroom.Subject.ToLower().Contains("english"))
      {
        fallbackWords = new List<string> { "school", "pencil", "friend", "apple" };
      }

      GeneratedAdaptiveChallengePreviewDto preview;
      if (vocabularyItems.Count > 0)
      {
        preview = await challengeOrchestrator.GenerateAsync(new GenerateAdaptiveChallengeRequest(
            "class",
            null,
            classroom.Id,
            "practice_words",
            "PREDEFINED_MODULE",
            targetModuleId,
            gameKey,
            "practice words",
            null,
            null,
            null), cancellationToken);
      }
      else
      {
        preview = await challengeOrchestrator.GenerateAsync(new GenerateAdaptiveChallengeRequest(
            "class",
            null,
            classroom.Id,
            "custom_challenge",
            "manual_input",
            targetModuleId,
            gameKey,
            "practice words",
            fallbackWords,
            null,
            null), cancellationToken);
      }

      var assigned = await challengeOrchestrator.AssignAsync(new AssignAdaptiveChallengeRequest(
          teacherId,
          null,
          classroom.Id,
          null,
          preview with { SourceType = "PREDEFINED_MODULE", ModuleId = targetModuleId },
          classroom.Subject,
          null), cancellationToken);

      var pdfBytes = await rosterPdfGenerator.GenerateRosterPdfAsync(classroom.Name, rosterPreview, cancellationToken);
      await transaction.CommitAsync(cancellationToken);

      var result = new SetupClassroomWizardResult(
          classroom.Id,
          classroom.Name,
          classroom.JoinCode,
          assigned.ChallengeId.ToString(),
          $"{classroom.Name.Replace(' ', '_')}_login_badges.pdf",
          Convert.ToBase64String(pdfBytes),
          rosterPreview);

      return OperationResult<SetupClassroomWizardResult>.SuccessResult(result);
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync(cancellationToken);
      return OperationResult<SetupClassroomWizardResult>.FailureResult($"Failed to complete wizard setup: {ex.Message}");
    }
  }

  private async Task<User> CreateStudentUserAsync(string studentName, CancellationToken cancellationToken)
  {
    var safeName = UnsafeUserNameChars.Replace(studentName.ToLowerInvariant(), string.Empty);
    if (string.IsNullOrWhiteSpace(safeName))
      safeName = "student";

    User user;
    while (true)
    {
      var suffix = RandomNumberGenerator.GetInt32(10000, 99999);
      var userName = $"{safeName}_{suffix}";

      var existing = await userManager.FindByNameAsync(userName);
      if (existing is not null)
        continue;

      user = new User
      {
        UserName = userName,
        Name = studentName,
        FamilyName = string.Empty,
        AvatarId = "0"
      };

      break;
    }

    var createResult = await userManager.CreateAsync(user);
    if (!createResult.Succeeded)
      throw new InvalidOperationException(createResult.Errors.FirstOrDefault()?.Description ?? "Unable to create student user");

    var roleResult = await userManager.AddToRoleAsync(user, "student");
    if (!roleResult.Succeeded)
      throw new InvalidOperationException(roleResult.Errors.FirstOrDefault()?.Description ?? "Unable to assign student role");

    return user;
  }

  private static string EnsureUniqueLoginCode(string preferredCode, HashSet<string> existingCodes)
  {
    if (existingCodes.Add(preferredCode))
      return preferredCode;

    while (true)
    {
      var fallbackCode = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
      if (existingCodes.Add(fallbackCode))
        return fallbackCode;
    }
  }

  private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken cancellationToken)
  {
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    while (true)
    {
      var buffer = new char[4];
      for (var i = 0; i < buffer.Length; i++)
        buffer[i] = chars[RandomNumberGenerator.GetInt32(0, chars.Length)];

      var joinCode = new string(buffer);
      var exists = await dbContext.Classrooms.AsNoTracking().AnyAsync(c => c.JoinCode == joinCode, cancellationToken);
      if (!exists)
        return joinCode;
    }
  }
}
