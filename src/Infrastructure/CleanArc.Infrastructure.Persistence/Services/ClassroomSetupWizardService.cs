using System.Security.Cryptography;
using System.Text.RegularExpressions;
using BCrypt.Net;
using CleanArc.Application.Contracts.Infrastructure;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Services;

public class ClassroomSetupWizardService(
    ApplicationDbContext dbContext,
    UserManager<User> userManager,
    IStudentImportService studentImportService,
    IRosterPdfGenerator rosterPdfGenerator)
    : IClassroomSetupWizardService
{
  private static readonly Regex UnsafeUserNameChars = new("[^a-zA-Z0-9]", RegexOptions.Compiled);

  public async Task<OperationResult<SetupClassroomWizardResult>> SetupClassroomAsync(
      int teacherId,
      string className,
      string subject,
      int challengeId,
      string csvContent,
      CancellationToken cancellationToken)
  {
    var challenge = await dbContext.Challenges
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == challengeId, cancellationToken);

    if (challenge is null)
      return OperationResult<SetupClassroomWizardResult>.NotFoundResult("Challenge template not found");

    var parsedStudents = studentImportService.ParseStudents(csvContent);
    if (parsedStudents.Count == 0)
      return OperationResult<SetupClassroomWizardResult>.FailureResult("CSV did not contain any valid student names");

    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    try
    {
      var classroom = new Classroom
      {
        Name = className.Trim(),
        Description = "Created with Classroom Setup Wizard",
        Subject = subject.Trim(),
        Thumbnail = string.Empty,
        JoinCode = await GenerateUniqueJoinCodeAsync(cancellationToken),
        TeacherId = teacherId,
        IsActive = true
      };

      dbContext.Classrooms.Add(classroom);
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
          VisualPasswordHash = BCrypt.Net.BCrypt.HashPassword(student.VisualPassword),
          IsActive = true,
          FailedAttempts = 0
        });

        rosterPreview.Add(new StudentCredentialPreview(student.StudentName, loginCode, student.VisualPassword));
      }

      dbContext.ClassroomQuizzes.Add(new ClassroomQuiz
      {
        ClassroomId = classroom.Id,
        QuizId = challengeId.ToString(),
        AssignedDate = DateTime.UtcNow
      });

      await dbContext.SaveChangesAsync(cancellationToken);

      var pdfBytes = await rosterPdfGenerator.GenerateRosterPdfAsync(classroom.Name, rosterPreview, cancellationToken);
      await transaction.CommitAsync(cancellationToken);

      var result = new SetupClassroomWizardResult(
          classroom.Id,
          classroom.Name,
          classroom.JoinCode,
          challengeId.ToString(),
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