using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

public record GetTeacherClassroomsQuery(int TeacherId, bool IncludeDeleted = false) : IRequest<OperationResult<List<ClassroomDto>>>;
