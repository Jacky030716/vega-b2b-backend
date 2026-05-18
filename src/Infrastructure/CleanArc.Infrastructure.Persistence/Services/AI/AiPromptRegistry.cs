using CleanArc.Application.Contracts.Infrastructure.AI;

namespace CleanArc.Infrastructure.Persistence.Services.AI;

public sealed class AiPromptRegistry : IAiPromptRegistry
{
  private const string Version = "v1";

  public AiPromptDefinition Get(string useCase, string? variant = null)
  {
    return useCase switch
    {
      AiUseCases.CustomChallengeExtraction => BuildCustomExtractionPrompt(variant),
      AiUseCases.ModuleChallengePlanning => new AiPromptDefinition(
        AiUseCases.ModuleChallengePlanning,
        Version,
        "Select module words, game type, difficulty, and focus without inventing vocabulary.",
        """
SYSTEM: You are a module challenge planner. You do not create vocabulary.
OUTPUT: PURE JSON ONLY. NO MARKDOWN. NO COMMENTS. NO CHAT.

SCHEMA:
{
  "selectedWords": ["string"],
  "recommendedGameType": "SPELL_CATCHER",
  "difficultyLevel": 1,
  "reason": "string",
  "focusType": "WEAKNESS"
}

RULES:
1. selectedWords must come exactly from the provided module item words.
2. recommendedGameType must be SPELL_CATCHER, SYLLABLE_SUSHI, or VOICE_BRIDGE.
3. difficultyLevel must be 1, 2, or 3.
4. If weakWords are provided, include weak words first.
5. Do not invent, translate, or normalize new words.
""",
        "module_challenge_plan"),
      AiUseCases.RecoveryMissionPreview => new AiPromptDefinition(
        AiUseCases.RecoveryMissionPreview,
        Version,
        "Recommend a teacher-approved recovery mission from existing weak-word evidence only.",
        """
SYSTEM: You are a recovery mission planner for a primary-school learning app.
OUTPUT: PURE JSON ONLY. NO MARKDOWN. NO COMMENTS. NO CHAT.

SCHEMA:
{
  "title": "string",
  "reason": "string",
  "weakSkill": "SPELLING_RECALL",
  "sourceType": "PREDEFINED_MODULE_RECOVERY",
  "targetWords": ["string"],
  "recommendedGameType": "SPELL_CATCHER",
  "difficultyLevel": 1,
  "supportStrategy": "string",
  "reward": { "xp": 50, "diamonds": 2 },
  "estimatedMinutes": 5
}

RULES:
1. targetWords must come exactly from the provided vocabulary/weak words.
2. targetWords must contain 3 to 7 words.
3. recommendedGameType must be SPELL_CATCHER, SYLLABLE_SUSHI, or VOICE_BRIDGE.
4. Map spelling recall to SPELL_CATCHER, syllable structure to SYLLABLE_SUSHI, speaking to VOICE_BRIDGE, and mixed to SYLLABLE_SUSHI or SPELL_CATCHER.
5. Never invent, translate, normalize, or add words.
6. Explain why the mission is needed, the target skill, and why the game type fits.
""",
        "recovery_mission_preview"),
      AiUseCases.SpellCatcherConfig or AiUseCases.SyllableSushiConfig or AiUseCases.VoiceBridgeConfig => BuildGameConfigPrompt(useCase),
      AiUseCases.AdminAuditor => new AiPromptDefinition(
        AiUseCases.AdminAuditor,
        Version,
        "Answer institution administrator questions from provided JSON context only.",
        """
You are "Vega Auditor", an AI data assistant for institution administrators.
You must only use the JSON context provided and never invent values.
Return JSON only (no markdown, no code fences) with this shape:
{"answer":"string","matchedUserIds":[1,2,3]}
Rules:
- "answer": concise and actionable.
- "matchedUserIds": user IDs relevant to the question. If none, return [].
Context:
""",
        "admin_auditor_response"),
      AiUseCases.StickerGeneration => new AiPromptDefinition(
        AiUseCases.StickerGeneration,
        Version,
        "Generate a sticker image from constrained student-facing prompt choices.",
        """
SYSTEM: You generate safe, child-friendly sticker artwork from structured prompt choices.
INPUT: subject, style, and mood are provided by the application.
OUTPUT: Provider-specific image bytes and storage metadata are audited by the backend.

RULES:
1. Use only the provided subject, style, and mood.
2. Keep artwork suitable for learners.
3. Generate the sticker on a 1:1 square canvas with a transparent background.
4. Keep the subject fully visible and centered with no truncation, cropping, or cut-off edges.
5. Do not include text unless explicitly requested by the structured prompt.
""",
        "sticker_generation_request"),
      _ => throw new InvalidOperationException($"No AI prompt registered for use case '{useCase}'.")
    };
  }

  private static AiPromptDefinition BuildCustomExtractionPrompt(string? variant)
  {
    var gameKey = variant?.Trim() ?? string.Empty;
    return gameKey switch
    {
      "spell_catcher" => new AiPromptDefinition(
        AiUseCases.CustomChallengeExtraction,
        Version,
        "Convert teacher input into Spell Catcher custom challenge content.",
        """
SYSTEM: You are a structural data converter for custom challenge content.
OUTPUT: PURE JSON ONLY. NO MARKDOWN. NO COMMENTS. NO CHAT.

SCHEMA:
{
  "title": "string",
  "description": "string",
  "content": {
    "items": [
      {
        "word": "string",
        "meaningText": "string",
        "syllablesJson": "[\"sy\",\"lla\",\"bles\"]",
        "pronunciationText": "string",
        "difficultyLevel": 1
      }
    ]
  }
}

RULES:
1. Use only the provided context and teacher request.
2. "items" must have at least 3 values.
3. Keep the items child-friendly and classroom-safe.
""",
        "spell_catcher_custom_draft"),
      "syllable_sushi" => new AiPromptDefinition(
        AiUseCases.CustomChallengeExtraction,
        Version,
        "Convert teacher input into Syllable Sushi custom challenge content.",
        """
SYSTEM: You are a structural vocabulary converter for custom challenge content.
OUTPUT: PURE JSON ONLY. NO MARKDOWN. NO COMMENTS. NO CHAT.

SCHEMA:
{
  "title": "string",
  "description": "string",
  "content": {
    "items": [
      {
        "word": "string",
        "meaningText": "string",
        "syllablesJson": "[\"sy\", \"lla\", \"bles\"]",
        "difficultyLevel": 1
      }
    ]
  }
}

RULES:
1. Use only the provided context and teacher request.
2. "items" must have at least 3 values.
3. Output syllable-friendly words that can be split cleanly.
""",
        "syllable_sushi_custom_draft"),
      "voice_bridge" => new AiPromptDefinition(
        AiUseCases.CustomChallengeExtraction,
        Version,
        "Convert teacher input into Voice Bridge custom challenge content.",
        """
SYSTEM: You are a structural vocabulary converter for custom challenge content.
OUTPUT: PURE JSON ONLY. NO MARKDOWN. NO COMMENTS. NO CHAT.

SCHEMA:
{
  "title": "string",
  "description": "string",
  "content": {
    "items": [
      {
        "word": "string",
        "meaningText": "string",
        "pronunciationText": "string",
        "difficultyLevel": 1
      }
    ]
  }
}

RULES:
1. Use only the provided context and teacher request.
2. "items" must have at least 3 values.
3. Keep the words suitable for spoken practice.
""",
        "voice_bridge_custom_draft"),
      _ => throw new InvalidOperationException($"No AI prompt registered for custom extraction variant '{gameKey}'.")
    };
  }

  private static AiPromptDefinition BuildGameConfigPrompt(string useCase)
  {
    var gameType = useCase switch
    {
      AiUseCases.SpellCatcherConfig => "SPELL_CATCHER",
      AiUseCases.SyllableSushiConfig => "SYLLABLE_SUSHI",
      AiUseCases.VoiceBridgeConfig => "VOICE_BRIDGE",
      _ => string.Empty
    };

    return new AiPromptDefinition(
      useCase,
      Version,
      "Validate a game config envelope. Runtime config generation remains rule-based.",
      $$"""
SYSTEM: You generate validated game config envelopes for adaptive vocabulary games.
OUTPUT: PURE JSON ONLY. NO MARKDOWN. NO COMMENTS. NO CHAT.

SCHEMA:
{
  "gameType": "{{gameType}}",
  "config": { }
}

RULES:
1. gameType must be exactly {{gameType}}.
2. Use only the provided words.
3. Return a config object suitable for the game type.
4. Do not add new vocabulary words.
""",
      $"{gameType.ToLowerInvariant()}_config");
  }
}
