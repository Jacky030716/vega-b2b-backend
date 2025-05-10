using System.Text.Json.Serialization;
using CleanArc.Application.Contracts.DTOs.Word;
using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.WordLists.Commands;

public record UpdateWordListCommand(int WordListId, string Title, List<CreateWordDto> Words) : IRequest<OperationResult<bool>>, IValidatableModel<UpdateWordListCommand>
{
    [JsonIgnore]
    public int UserId { get; set; }

    public IValidator<UpdateWordListCommand> ValidateApplicationModel(ApplicationBaseValidationModelProvider<UpdateWordListCommand> validator)
    {
        validator.RuleFor(c => c.WordListId)
            .NotEmpty()
            .GreaterThan(0);
        validator.RuleFor(c => c.Title)
            .NotEmpty()
            .NotNull();
        return validator;
    }
};
