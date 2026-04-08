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
    int ChallengeId,
    string CsvContent)
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

    validator.RuleFor(c => c.ChallengeId)
        .GreaterThan(0)
        .WithMessage("Challenge ID must be a positive number");

    validator.RuleFor(c => c.CsvContent)
        .NotEmpty()
        .WithMessage("CSV file is required");

    return validator;
  }
}