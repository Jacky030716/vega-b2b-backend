using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Classrooms.Commands;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Institution;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Classrooms;

public class CreateClassroomMetadataTests
{
  [Fact]
  public async Task CreateClassroom_DoesNotRequireSubjectOrYearLevelMetadata()
  {
    var classroomRepository = Substitute.For<IClassroomRepository>();
    var institutionRepository = Substitute.For<IInstitutionRepository>();
    var unitOfWork = Substitute.For<IUnitOfWork>();
    unitOfWork.ClassroomRepository.Returns(classroomRepository);
    unitOfWork.InstitutionRepository.Returns(institutionRepository);

    institutionRepository.GetPrimaryInstitutionForUserAsync(42, Arg.Any<CancellationToken>())
        .Returns(new InstitutionUser
        {
          InstitutionId = 7,
          UserId = 42,
          Institution = new Institution
          {
            Id = 7,
            Name = "Vega School",
            SubscriptionTier = "Premium",
            StripeCustomerId = string.Empty
          }
        });

    classroomRepository.CreateClassroomAsync(Arg.Any<Classroom>())
        .Returns(call =>
        {
          var classroom = call.Arg<Classroom>();
          return classroom;
        });

    var handler = new CreateClassroomCommandHandler(unitOfWork);

    var result = await handler.Handle(new CreateClassroomCommand(
        42,
        "Open Language Lab",
        "Any language can be added later.",
        null), CancellationToken.None);

    Assert.True(result.IsSuccess);
    await classroomRepository.DidNotReceiveWithAnyArgs()
        .ProvisionClassroomModulesAsync(default, default!, default);
  }
}
