using AutoMapper;
using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Models.Common;
using CleanArc.Domain.Entities.User;
using Mediator;
using Microsoft.Extensions.Logging;

namespace CleanArc.Application.Features.Users.Commands.Create;

internal class UserCreateCommandHandler : IRequestHandler<UserCreateCommand, OperationResult<UserCreateCommandResult>>
{

    private readonly IAppUserManager _userManager;
    private readonly ILogger<UserCreateCommandHandler> _logger;
    private readonly IMapper _mapper;
    public UserCreateCommandHandler(IAppUserManager userRepository, ILogger<UserCreateCommandHandler> logger, IMapper mapper)
    {
        _userManager = userRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async ValueTask<OperationResult<UserCreateCommandResult>> Handle(UserCreateCommand request, CancellationToken cancellationToken)
    {
        // Check if username already exists
        var userNameExist = await _userManager.IsExistUserName(request.UserName);
        if (userNameExist)
            return OperationResult<UserCreateCommandResult>.FailureResult("Username already exists");

        // Check if email already exists
        var emailUser = await _userManager.FindUserByEmail(request.Email);
        if (emailUser != null)
            return OperationResult<UserCreateCommandResult>.FailureResult("Email already exists");

        // Check if phone number exists (if provided)
        if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            var phoneExist = await _userManager.IsExistUser(request.PhoneNumber);
            if (phoneExist)
                return OperationResult<UserCreateCommandResult>.FailureResult("Phone number already exists");
        }

        // Create user object
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            Name = request.Name,
            FamilyName = request.FamilyName,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true, // Auto-confirm since admin is creating
            PhoneNumberConfirmed = !string.IsNullOrEmpty(request.PhoneNumber)
        };

        // Create user with password
        var createResult = await _userManager.CreateUserWithPasswordAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            return OperationResult<UserCreateCommandResult>.FailureResult(string.Join(",", createResult.Errors.Select(c => c.Description)));
        }

        // Assign role
        var role = new CleanArc.Domain.Entities.User.Role { Name = request.Role };
        var addRoleResult = await _userManager.AddUserToRoleAsync(user, role);

        if (!addRoleResult.Succeeded)
        {
            _logger.LogError($"Failed to assign role {request.Role} to user {user.UserName}");
        }

        _logger.LogInformation($"User {user.UserName} created successfully with role {request.Role}");

        return OperationResult<UserCreateCommandResult>.SuccessResult(new UserCreateCommandResult { UserGeneratedKey = user.Id.ToString() });
    }
}