using CleanArc.Application.Contracts.DTOs.User;
using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Users.Commands.UpdateUserProfile;

public record UpdateUserProfileCommand(int UserId, UpdateUserProfileRequest Profile)
    : IRequest<OperationResult<UpdateUserProfileResponse>>,
    IValidatableModel<UpdateUserProfileCommand>
{
  public IValidator<UpdateUserProfileCommand> ValidateApplicationModel(
      ApplicationBaseValidationModelProvider<UpdateUserProfileCommand> validator)
  {
    validator.RuleFor(c => c.UserId)
        .GreaterThan(0)
        .WithMessage("Invalid user ID");

    validator.RuleFor(c => c.Profile)
        .NotNull()
        .WithMessage("Profile data is required");

    validator.RuleFor(c => c.Profile.Name)
        .NotEmpty()
        .NotNull()
        .MaximumLength(100)
        .WithMessage("Name must be provided and less than 100 characters");

    validator.RuleFor(c => c.Profile.FamilyName)
        .NotEmpty()
        .NotNull()
        .MaximumLength(100)
        .WithMessage("Family name must be provided and less than 100 characters");

    validator.RuleFor(c => c.Profile.AvatarId)
        .MaximumLength(50)
        .WithMessage("Avatar ID must be less than 50 characters");

    return validator;
  }
}
