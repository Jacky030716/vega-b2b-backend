
using System.Text.Json.Serialization;
using CleanArc.Application.Contracts.DTOs.Word;
using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.ValidationBase;
using CleanArc.SharedKernel.ValidationBase.Contracts;
using FluentValidation;
using Mediator;

namespace CleanArc.Application.Features.WordLists.Commands
{
    public record CreateWordListCommand(string Title, List<CreateWordDto> Words) : IRequest<OperationResult<bool>>, IValidatableModel<CreateWordListCommand>
    {
        [JsonIgnore]
        public int UserId { get; set; }

        public IValidator<CreateWordListCommand> ValidateApplicationModel(ApplicationBaseValidationModelProvider<CreateWordListCommand> validator)
        {
            validator.RuleFor(c => c.Title)
                .NotEmpty()
                .NotNull()
                .WithMessage("Please enter your word list title");
            validator.RuleFor(c => c.Words)
                .NotEmpty()
                .NotNull()
                .WithMessage("Please enter your words");

            return validator;
        }
    }
}
