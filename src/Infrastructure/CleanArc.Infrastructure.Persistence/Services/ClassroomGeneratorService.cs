using System.Text;
using CleanArc.Application.Contracts.Infrastructure;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace CleanArc.Infrastructure.Persistence.Services;

public class ClassroomGeneratorService : IClassroomGeneratorService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;

    public ClassroomGeneratorService(ApplicationDbContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<OperationResult<byte[]>> BulkCreateClassroomAsync(string name, string description, string subject, string csvContent, int teacherId, CancellationToken cancellationToken)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var studentNames = ParseStudents(csvContent);
        if (!studentNames.Any()) return OperationResult<byte[]>.FailureResult("No students found in the input.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var classroom = new CleanArc.Domain.Entities.Classroom.Classroom
            {
                Name = name,
                Description = description ?? "",
                Subject = subject ?? "",
                TeacherId = teacherId,
                JoinCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper()
            };

            _dbContext.Classrooms.Add(classroom);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var random = new Random();
            var credentials = new List<StudentCredentialRecord>();

            foreach (var studentName in studentNames)
            {
                var user = new User
                {
                    UserName = $"{studentName.Replace(" ", "").ToLower()}_{random.Next(10000, 99999)}",
                    Name = studentName,
                    AvatarId = "0"
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                    throw new Exception($"Failed to create student user: {result.Errors.FirstOrDefault()?.Description}");

                // Removed RoleExistsAsync check as RoleManager is not injected

                _dbContext.ClassroomStudents.Add(new ClassroomStudent
                {
                    ClassroomId = classroom.Id,
                    UserId = user.Id
                });

                var loginCode = random.Next(1000, 9999).ToString();

                _dbContext.StudentCredentials.Add(new StudentCredential
                {
                    UserId = user.Id,
                    ClassroomId = classroom.Id,
                    StudentLoginCode = loginCode,
                    VisualPasswordHash = "DEFAULT" // required by existing DB constraints
                });

                var qrCodeBase64 = GenerateQRCode(loginCode);
                credentials.Add(new StudentCredentialRecord(studentName, loginCode, qrCodeBase64));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var pdfBytes = GeneratePdfRoster(name, credentials);
            return OperationResult<byte[]>.SuccessResult(pdfBytes);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OperationResult<byte[]>.FailureResult($"Failed to bulk create classroom: {ex.Message}");
        }
    }

    private List<string> ParseStudents(string content)
    {
        return content.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(x => x.Trim())
                      .Where(x => !string.IsNullOrEmpty(x))
                      .ToList();
    }

    private string GenerateQRCode(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(10);
        return Convert.ToBase64String(qrCodeImage);
    }

    private byte[] GeneratePdfRoster(string classroomName, List<StudentCredentialRecord> students)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);

                page.Header()
                    .Text($"{classroomName} - Roster")
                    .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2);

                page.Content()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        foreach (var student in students)
                        {
                            table.Cell().Padding(5).Element(cell =>
                            {
                                cell.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(col =>
                                {
                                    col.Item().Text(student.Name).Bold().FontSize(14).AlignCenter();
                                    col.Item().Text($"Code: {student.Code}").FontSize(12).AlignCenter();

                                    var imageBytes = Convert.FromBase64String(student.QrBase64);
                                    col.Item().Height(100).Image(imageBytes).FitArea();
                                });
                            });
                        }
                    });
            });
        }).GeneratePdf();
    }
}

public record StudentCredentialRecord(string Name, string Code, string QrBase64);
