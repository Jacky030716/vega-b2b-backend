using CleanArc.Application.Contracts.DTOs.Word;

namespace CleanArc.Application.Contracts.DTOs;

public class WordListDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public List<CreateWordDto> Words { get; set; } = new();
}
