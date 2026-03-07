using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

public record GetStudentClassroomsQuery(int UserId) : IRequest<OperationResult<List<ClassroomDto>>>;

public record ClassroomDto(int Id, string Name, string Description, string Subject, string? Thumbnail, string JoinCode, int TeacherId, string TeacherName, int StudentCount, int QuizCount);
