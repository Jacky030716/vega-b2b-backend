using System.Text.Json.Serialization;
using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.WordLists.Commands;

public record DeleteWordListCommand(int WordListId) : IRequest<OperationResult<bool>>, IValidatableModel<DeleteWordListCommand>
{
    [JsonIgnore]
    public int UserId { get; set; }

    public IValidator<DeleteWordListCommand> ValidateApplicationModel(ApplicationBaseValidationModelProvider<DeleteWordListCommand> validator)
    {
        validator.RuleFor(c => c.WordListId)
            .NotEmpty()
            .GreaterThan(0);
        return validator;
    }
}
