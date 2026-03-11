using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using Mediator;
using System.Text.Json.Nodes;

namespace CleanArc.Application.Features.Games.Queries;

internal class GetChallengesForGameQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetChallengesForGameQuery, OperationResult<List<ChallengeNodeDto>>>
{
  public async ValueTask<OperationResult<List<ChallengeNodeDto>>> Handle(
      GetChallengesForGameQuery request, CancellationToken cancellationToken)
  {
    var game = await unitOfWork.ChallengeRepository.GetGameByKeyAsync(request.GameKey);
    if (game is null)
      return OperationResult<List<ChallengeNodeDto>>.NotFoundResult($"Game '{request.GameKey}' not found");

    var challenges = await unitOfWork.ChallengeRepository.GetChallengesForGameAsync(game.Id);
    var bestAttempts = await unitOfWork.ChallengeRepository.GetUserBestAttemptsForGameAsync(request.UserId, game.Id);

    // Build a quick lookup: challengeId → best attempt
    var attemptMap = bestAttempts.ToDictionary(a => a.ChallengeId);

    var nodes = new List<ChallengeNodeDto>(challenges.Count);
    for (int i = 0; i < challenges.Count; i++)
    {
      var c = challenges[i];
      var hasBest = attemptMap.TryGetValue(c.Id, out var best);

      int bestStars = hasBest ? best!.StarsEarned : 0;
      bool isCompleted = hasBest && best!.IsCompleted;

      // Node 1 is always unlocked; subsequent nodes unlock when the previous is completed.
      bool isUnlocked = i == 0
          || (attemptMap.TryGetValue(challenges[i - 1].Id, out var prevBest) && prevBest.IsCompleted);

      nodes.Add(new ChallengeNodeDto(
          Id: c.Id,
          GameId: c.GameId,
          GameKey: game.Key,
          Title: c.Title,
          Description: c.Description,
          DifficultyLevel: c.DifficultyLevel,
          OrderIndex: c.OrderIndex,
          MaxStars: c.MaxStars,
          BestStars: bestStars,
          IsCompleted: isCompleted,
          IsUnlocked: isUnlocked,
          ContentData: NormalizeContentData(c.ContentData)
      ));
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
      // Keep endpoint resilient if legacy rows contain malformed JSON.
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
