using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Games.Commands;
using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Quiz;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Games;

public class CreateChallengeCommandHandlerTests
{
    [Fact]
    public async Task Handle_AllowsTranslationChallengeOnCustomModuleBeyondSyllabusCap()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var challengeRepository = Substitute.For<IChallengeRepository>();
        var classroomRepository = Substitute.For<IClassroomRepository>();
        unitOfWork.ChallengeRepository.Returns(challengeRepository);
        unitOfWork.ClassroomRepository.Returns(classroomRepository);

        var game = WithId(new Game { Key = "translation", Name = "Translation" }, 7);
        challengeRepository.GetGameByKeyAsync("translation")
            .Returns(game);
        challengeRepository.GetNextOrderIndexForGameAsync(7).Returns(11);
        challengeRepository.CreateChallengeAsync(Arg.Any<Challenge>())
            .Returns(call =>
            {
                var challenge = call.Arg<Challenge>();
                return WithId(challenge, 99);
            });

        classroomRepository.IsModuleAttachedToClassroomAsync(12, 34).Returns(true);
        classroomRepository.GetAttachedModuleTypeAsync(12, 34).Returns(SyllabusModule.CustomModuleType);
        challengeRepository.CountActiveModuleChallengesAsync(12, 34).Returns(8);

        var handler = new CreateChallengeCommandHandler(unitOfWork, Substitute.For<IAiAuditService>());

        var result = await handler.Handle(
            new CreateChallengeCommand(
                UserId: 5,
                GameKey: "translation",
                Title: "Custom Translation",
                Description: "AI Hub prompt challenge",
                DifficultyLevel: 2,
                ContentData: TranslationContentData(),
                IsAIGenerated: true,
                CreationMode: "prompt",
                SourcePrompt: "Create translation practice",
                SourceDocumentName: null,
                ClassroomId: 12,
                ModuleId: 34,
                AiAuditLogId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(99, result.Result!.ChallengeId);
        Assert.Equal(34, result.Result.ModuleId);
        _ = challengeRepository.DidNotReceive().CountActiveModuleChallengesAsync(12, 34);
    }

    [Fact]
    public async Task Handle_StillAppliesChallengeCapToPredefinedModules()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var challengeRepository = Substitute.For<IChallengeRepository>();
        var classroomRepository = Substitute.For<IClassroomRepository>();
        unitOfWork.ChallengeRepository.Returns(challengeRepository);
        unitOfWork.ClassroomRepository.Returns(classroomRepository);

        var game = WithId(new Game { Key = "translation", Name = "Translation" }, 7);
        challengeRepository.GetGameByKeyAsync("translation")
            .Returns(game);
        challengeRepository.GetNextOrderIndexForGameAsync(7).Returns(11);
        classroomRepository.IsModuleAttachedToClassroomAsync(12, 34).Returns(true);
        classroomRepository.GetAttachedModuleTypeAsync(12, 34).Returns(SyllabusModule.PredefinedModuleType);
        challengeRepository.CountActiveModuleChallengesAsync(12, 34).Returns(3);

        var handler = new CreateChallengeCommandHandler(unitOfWork, Substitute.For<IAiAuditService>());

        var result = await handler.Handle(
            new CreateChallengeCommand(
                UserId: 5,
                GameKey: "translation",
                Title: "Module Translation",
                Description: "Syllabus challenge",
                DifficultyLevel: 2,
                ContentData: TranslationContentData(),
                IsAIGenerated: true,
                CreationMode: "prompt",
                SourcePrompt: "Create translation practice",
                SourceDocumentName: null,
                ClassroomId: 12,
                ModuleId: 34,
                AiAuditLogId: null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Each module can have up to 3 game challenges", result.ErrorMessage);
        _ = challengeRepository.DidNotReceive().CreateChallengeAsync(Arg.Any<Challenge>());
    }

    private static string TranslationContentData()
    {
        return """
        {
          "sourceLanguage": "ms",
          "targetLanguage": "en",
          "items": [
            { "word": "apple", "meaningText": "epal" },
            { "word": "book", "meaningText": "buku" },
            { "word": "school", "meaningText": "sekolah" }
          ]
        }
        """;
    }

    private static T WithId<T>(T entity, int id) where T : BaseEntity<int>
    {
        typeof(BaseEntity<int>)
            .GetProperty(nameof(BaseEntity<int>.Id))!
            .SetValue(entity, id);

        return entity;
    }
}
