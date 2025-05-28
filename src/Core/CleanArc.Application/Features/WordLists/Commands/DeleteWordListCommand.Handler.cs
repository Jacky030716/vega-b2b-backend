using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.WordLists.Commands;

public class DeleteWordListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteWordListCommand, OperationResult<bool>>
{
    public async ValueTask<OperationResult<bool>> Handle(DeleteWordListCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var wordListId = request.WordListId;

        await unitOfWork.WordListRepository.DeleteUserWordListAsync(wordListId, userId);

        return OperationResult<bool>.SuccessResult(true);
    }
}
