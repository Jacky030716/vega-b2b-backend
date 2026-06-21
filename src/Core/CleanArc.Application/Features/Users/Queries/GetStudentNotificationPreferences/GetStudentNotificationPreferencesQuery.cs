using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.GetStudentNotificationPreferences;

public record GetStudentNotificationPreferencesQuery(int StudentId)
    : IRequest<OperationResult<StudentNotificationPreferencesDto>>;
