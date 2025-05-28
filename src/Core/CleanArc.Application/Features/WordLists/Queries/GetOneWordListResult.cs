using CleanArc.Application.Contracts.DTOs.Word;

namespace CleanArc.Application.Features.WordLists.Queries;

public record GetOneWordListResult(int WordlistId, string WordlistTitle, List<CreateWordDto> Words)
{
}
