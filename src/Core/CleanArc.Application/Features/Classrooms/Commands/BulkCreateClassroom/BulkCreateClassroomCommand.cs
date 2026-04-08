using Mediator;
using CleanArc.Application.Models.Common;

namespace CleanArc.Application.Features.Classrooms.Commands.BulkCreateClassroom;

public class BulkCreateClassroomCommand : IRequest<OperationResult<byte[]>>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Subject { get; set; }
    public string CsvContent { get; set; }
    public int TeacherId { get; set; }
}
