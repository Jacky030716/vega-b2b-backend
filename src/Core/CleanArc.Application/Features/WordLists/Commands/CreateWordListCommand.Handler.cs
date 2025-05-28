using CleanArc.Application.Contracts.Identity;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.WordLists.Commands
{
    public class CreateWordListCommandHandler : IRequestHandler<CreateWordListCommand, OperationResult<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppUserManager _userManager;

        public CreateWordListCommandHandler(IUnitOfWork unitOfWork, IAppUserManager userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async ValueTask<OperationResult<bool>> Handle(CreateWordListCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserByIdAsync(request.UserId);

            if (user == null)
                return OperationResult<bool>.FailureResult("User Not Found");

            await _unitOfWork.WordListRepository.CreateWordListAsync(new Domain.Entities.Word.WordList()
            {
                UserId = user.Id,
                Title = request.Title,
                Words = request.Words.Select(w => new Domain.Entities.Word.Word()
                {
                    Text = w.Text,
                    ImageUrl = w.ImageUrl
                }).ToList()
            });

            await _unitOfWork.CommitAsync();

            return OperationResult<bool>.SuccessResult(true);
        }
    }
}
