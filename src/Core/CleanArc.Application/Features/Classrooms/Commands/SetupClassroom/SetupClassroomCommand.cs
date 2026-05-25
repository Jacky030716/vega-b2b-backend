using System.Collections.Generic;
using CleanArc.Application.Contracts.Infrastructure;
using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands.SetupClassroom;

public record SetupClassroomCommand(
    int TeacherId,
    string ClassName,
    string Subject,
    string GameKey,
    string CsvContent,
    int YearLevel = 1,
    IReadOnlyList<string>? Subjects = null)
    : IRequest<OperationResult<SetupClassroomWizardResult>>,
      IValidatableModel<SetupClassroomCommand>
{
  public IValidator<SetupClassroomCommand> ValidateApplicationModel(
      ApplicationBaseValidationModelProvider<SetupClassroomCommand> validator)
  {
    validator.RuleFor(c => c.ClassName)
        .NotEmpty()
        .MaximumLength(120)
        .WithMessage("Class name is required and must be under 120 characters");

    validator.RuleFor(c => c.Subject)
        .NotEmpty()
        .MaximumLength(80)
        .WithMessage("Subject is required and must be under 80 characters");

    validator.RuleFor(c => c.GameKey)
        .NotEmpty()
        .WithMessage("Game Key is required");

    validator.RuleFor(c => c.YearLevel)
        .InclusiveBetween(1, 6)
        .WithMessage("Year level must be between 1 and 6");

    validator.RuleFor(c => c.CsvContent)
        .NotEmpty()
        .WithMessage("CSV file is required");

    return validator;
  }
}