using System.Text.RegularExpressions;
using CleanArc.Application.Models.Common;
using CleanArc.Application.Profiles;
using CleanArc.Domain.Entities.User;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArc.Application.Features.Users.Commands.Create;

public record UserCreateCommand
    (string UserName, string Email, string Password, string Name, string FamilyName, string Role, string? PhoneNumber = null)
    : IRequest<OperationResult<UserCreateCommandResult>>
        , IValidatableModel<UserCreateCommand>
, ICreateMapper<User>
{

    public IValidator<UserCreateCommand> ValidateApplicationModel(ApplicationBaseValidationModelProvider<UserCreateCommand> validator)
    {

        validator
            .RuleFor(c => c.Name)
            .NotEmpty()
            .NotNull()
            .WithMessage("User must have first name");

        validator.RuleFor(c => c.UserName)
            .NotEmpty()
            .NotNull()
            .WithMessage("Please enter your username");

        validator
            .RuleFor(c => c.FamilyName)
            .NotEmpty()
            .NotNull()
            .WithMessage("User must have last name");

        validator.RuleFor(c => c.Email)
            .NotEmpty()
            .NotNull().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        validator.RuleFor(c => c.Password)
            .NotEmpty()
            .NotNull().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        validator.RuleFor(c => c.Role)
            .NotEmpty()
            .NotNull().WithMessage("Role is required.")
            .Must(r => r == "student" || r == "teacher" || r == "admin").WithMessage("Role must be either 'student', 'teacher', or 'admin'.");

        validator.RuleFor(c => c.PhoneNumber)
            .Must(phone => string.IsNullOrEmpty(phone) || Regex.IsMatch(phone, @"^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}$"))
            .WithMessage("Phone number is not valid.");

        return validator;
    }
}