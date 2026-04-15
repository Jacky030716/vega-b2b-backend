using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

public record GetClassroomDetailQuery(int ClassroomId, int UserId) : IRequest<OperationResult<ClassroomDetailDto>>;

public record ClassroomDetailDto(int Id, string Name, string Description, string Subject, string? Thumbnail, string JoinCode, int TeacherId, string TeacherName, int StudentCount, List<ClassroomQuizDto> Quizzes);

public record ClassroomQuizDto(int Id, string QuizId, DateTime AssignedDate, DateTime? DueDate, string? Title, string? GameKey);
