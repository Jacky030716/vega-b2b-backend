using CleanArc.Application.Contracts.Infrastructure;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Commands.BulkCreateClassroom;

public class BulkCreateClassroomCommandHandler : IRequestHandler<BulkCreateClassroomCommand, OperationResult<byte[]>>
{
    private readonly IClassroomGeneratorService _classroomGenerator;

    public BulkCreateClassroomCommandHandler(IClassroomGeneratorService classroomGenerator)
    {
        _classroomGenerator = classroomGenerator;
    }

    public async ValueTask<OperationResult<byte[]>> Handle(BulkCreateClassroomCommand request, CancellationToken cancellationToken)
    {
        return await _classroomGenerator.BulkCreateClassroomAsync(
            request.Name,
            request.Description,
            request.Subject,
            request.CsvContent,
            request.TeacherId,
            cancellationToken);
    }
}
