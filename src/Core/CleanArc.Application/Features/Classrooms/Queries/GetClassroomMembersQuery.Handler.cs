using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal class GetClassroomMembersQueryHandler : IRequestHandler<GetClassroomMembersQuery, OperationResult<List<ClassroomMemberDto>>>
{
  private readonly IUnitOfWork _unitOfWork;

  public GetClassroomMembersQueryHandler(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  public async ValueTask<OperationResult<List<ClassroomMemberDto>>> Handle(GetClassroomMembersQuery request, CancellationToken cancellationToken)
  {
    var classroom = await _unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
    if (classroom == null)
      return OperationResult<List<ClassroomMemberDto>>.NotFoundResult("Classroom not found");

    var members = await _unitOfWork.ClassroomRepository.GetClassroomMembersAsync(request.ClassroomId);
    var avatarItems = await _unitOfWork.ShopRepository.GetShopItemsAsync("avatar");
    var avatarUrlById = avatarItems.ToDictionary(item => item.Id, item => item.ImageUrl);

    static string? ResolveAvatarForResponse(string? rawAvatarId, IReadOnlyDictionary<int, string> avatarMap)
    {
      if (string.IsNullOrWhiteSpace(rawAvatarId) || rawAvatarId == "0")
        return "bear";

      if (rawAvatarId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
          rawAvatarId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        return rawAvatarId;

      if (int.TryParse(rawAvatarId, out var avatarItemId) && avatarMap.TryGetValue(avatarItemId, out var imageUrl))
        return imageUrl;

      return "bear";
    }

    var dtos = members
        .OrderByDescending(m => m.User.Experience)
        .Select(m => new ClassroomMemberDto(
            m.User.Id,
            m.User.UserName,
            m.User.Name is { Length: > 0 } ? m.User.Name : m.User.UserName,
            ResolveAvatarForResponse(m.User.AvatarId, avatarUrlById),
            m.User.Experience,
            m.User.Diamonds,
            m.JoinedDate
        ))
        .ToList();

    return OperationResult<List<ClassroomMemberDto>>.SuccessResult(dtos);
  }
}
