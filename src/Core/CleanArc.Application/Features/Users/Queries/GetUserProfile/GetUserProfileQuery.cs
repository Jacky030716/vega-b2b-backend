using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.GetUserProfile;

public record GetUserProfileQuery(int UserId) : IRequest<OperationResult<GetUserProfileResponse>>;
