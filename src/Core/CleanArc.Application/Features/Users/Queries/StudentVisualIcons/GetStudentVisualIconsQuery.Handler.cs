using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Users.Queries.StudentVisualIcons;

internal class GetStudentVisualIconsQueryHandler : IRequestHandler<GetStudentVisualIconsQuery, OperationResult<List<VisualIconDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetStudentVisualIconsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<OperationResult<List<VisualIconDto>>> Handle(GetStudentVisualIconsQuery request, CancellationToken cancellationToken)
    {
        var icons = await _unitOfWork.VisualIconRepository.GetAllAsync(cancellationToken);
        
        var dtoList = icons.Select(i => new VisualIconDto(i.Id, i.Emoji, i.Label)).ToList();

        return OperationResult<List<VisualIconDto>>.SuccessResult(dtoList);
    }
}
