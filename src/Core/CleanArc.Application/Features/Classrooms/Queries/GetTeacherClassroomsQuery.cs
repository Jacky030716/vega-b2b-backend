using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

public record GetTeacherClassroomsQuery(int TeacherId) : IRequest<OperationResult<List<ClassroomDto>>>;
