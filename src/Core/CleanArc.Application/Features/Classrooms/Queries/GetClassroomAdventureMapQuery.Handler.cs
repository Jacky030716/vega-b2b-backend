using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Games.Queries;
using CleanArc.Application.Models.Common;
using Mediator;
using System.Text.Json.Nodes;

namespace CleanArc.Application.Features.Classrooms.Queries;

internal class GetClassroomAdventureMapQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetClassroomAdventureMapQuery, OperationResult<List<ChallengeNodeDto>>>
{
  public async ValueTask<OperationResult<List<ChallengeNodeDto>>> Handle(
      GetClassroomAdventureMapQuery request,
      CancellationToken cancellationToken)
  {
    var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(request.ClassroomId);
    if (classroom is null)
      return OperationResult<List<ChallengeNodeDto>>.NotFoundResult("Classroom not found");

    var membership = await unitOfWork.ClassroomRepository.GetClassroomStudentAsync(request.ClassroomId, request.UserId);
    if (membership is null)
      return OperationResult<List<ChallengeNodeDto>>.UnauthorizedResult("You do not belong to this classroom");

    var classroomChallenges = await unitOfWork.ClassroomRepository.GetClassroomChallengesAsync(request.ClassroomId);
    if (classroomChallenges.Count == 0)
      return OperationResult<List<ChallengeNodeDto>>.SuccessResult(new List<ChallengeNodeDto>());

    var requestedGameKey = request.GameKey?.Trim() ?? string.Empty;

    var assignedChallenges = classroomChallenges
        .OrderBy(c => c.OrderIndex)
        .ThenBy(c => c.DifficultyLevel)
        .ToList();

    var fallbackGameKey = assignedChallenges
        .Select(c => c.Game?.Key)
        .FirstOrDefault(key => !string.IsNullOrWhiteSpace(key))
        ?? requestedGameKey;

    var nodes = new List<ChallengeNodeDto>(assignedChallenges.Count);
    var previousCompleted = false;

    for (var index = 0; index < assignedChallenges.Count; index++)
    {
      var challenge = assignedChallenges[index];
      var progress = await unitOfWork.ChallengeRepository
          .GetStudentChallengeProgressAsync(request.UserId, challenge.Id, request.ClassroomId);

      var isCompleted = progress?.HasCompleted == true;
      var isUnlocked = index == 0 || previousCompleted;

      nodes.Add(new ChallengeNodeDto(
          Id: challenge.Id,
          GameId: challenge.GameId,
          GameKey: challenge.Game?.Key ?? fallbackGameKey,
          Title: challenge.Title,
          Description: challenge.Description,
          DifficultyLevel: challenge.DifficultyLevel,
          OrderIndex: challenge.OrderIndex,
          MaxStars: challenge.MaxStars,
          BestStars: progress?.BestStars ?? 0,
          IsCompleted: isCompleted,
          IsUnlocked: isUnlocked,
          ContentData: NormalizeContentData(challenge.ContentData)
      ));

      previousCompleted = isCompleted;
    }

    return OperationResult<List<ChallengeNodeDto>>.SuccessResult(nodes);
  }

  private static string NormalizeContentData(string rawJson)
  {
    if (string.IsNullOrWhiteSpace(rawJson))
      return "{}";

    try
    {
      var parsed = JsonNode.Parse(rawJson);
      var normalized = NormalizeJsonNode(parsed);
      return normalized?.ToJsonString() ?? "{}";
    }
    catch
    {
      return rawJson;
    }
  }

  private static JsonNode? NormalizeJsonNode(JsonNode? node)
  {
    if (node is null) return null;

    if (node is JsonObject obj)
    {
      var normalizedObject = new JsonObject();
      foreach (var kv in obj)
      {
        var camelKey = ToCamelCase(kv.Key);
        normalizedObject[camelKey] = NormalizeJsonNode(kv.Value);
      }

      return normalizedObject;
    }

    if (node is JsonArray arr)
    {
      var normalizedArray = new JsonArray();
      foreach (var item in arr)
      {
        normalizedArray.Add(NormalizeJsonNode(item));
      }

      return normalizedArray;
    }

    return node.DeepClone();
  }

  private static string ToCamelCase(string key)
  {
    if (string.IsNullOrEmpty(key) || !char.IsUpper(key[0]))
      return key;

    if (key.Length == 1)
      return key.ToLowerInvariant();

    return char.ToLowerInvariant(key[0]) + key[1..];
  }
}
