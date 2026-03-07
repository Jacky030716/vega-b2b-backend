using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands;

public record JoinClassroomCommand(int UserId, string JoinCode) : IRequest<OperationResult<int>>;
