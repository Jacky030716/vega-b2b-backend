using CleanArc.Application.Contracts.Infrastructure.Billing;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Features.Admin.Billing;
using CleanArc.Domain.Entities.Billing;
using CleanArc.Domain.Entities.Institution;
using NSubstitute;

namespace CleanArc.Tests.Setup.Features.Admin.Billing;

public class AdminBillingTests
{
    [Fact]
    public async Task GetBillingSummary_ReturnsForbidden_WhenInstitutionMembershipCannotBeResolved()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.InstitutionRepository
            .GetPrimaryInstitutionForUserAsync(42, Arg.Any<CancellationToken>())
            .Returns((InstitutionUser?)null);
        var billingRepository = Substitute.For<IBillingRepository>();
        var billingPaymentService = Substitute.For<IBillingPaymentService>();
        var handler = new GetBillingSummaryQueryHandler(
            unitOfWork,
            billingRepository,
            billingPaymentService);

        var result = await handler.Handle(new GetBillingSummaryQuery(42), CancellationToken.None);

        Assert.True(result.IsForbidden);
        await unitOfWork.InstitutionRepository
            .DidNotReceive()
            .GetInstitutionWithStatsAsync(1);
    }

    [Fact]
    public async Task CreateMockWalletPayment_ReturnsForbidden_WhenInstitutionMembershipCannotBeResolved()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.InstitutionRepository
            .GetPrimaryInstitutionForUserAsync(42, Arg.Any<CancellationToken>())
            .Returns((InstitutionUser?)null);
        var billingRepository = Substitute.For<IBillingRepository>();
        var handler = new CreateMockWalletPaymentCommandHandler(unitOfWork, billingRepository);

        var result = await handler.Handle(
            new CreateMockWalletPaymentCommand(42, "grabpay"),
            CancellationToken.None);

        Assert.True(result.IsForbidden);
        await unitOfWork.InstitutionRepository
            .DidNotReceive()
            .GetInstitutionWithStatsAsync(1);
    }

    [Fact]
    public async Task CreateMockWalletPayment_RejectsUnsupportedWalletMethod()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var billingRepository = Substitute.For<IBillingRepository>();
        var handler = new CreateMockWalletPaymentCommandHandler(unitOfWork, billingRepository);

        var result = await handler.Handle(
            new CreateMockWalletPaymentCommand(42, "boost"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unsupported demo wallet payment method.", result.ErrorMessage);
        await unitOfWork.InstitutionRepository
            .DidNotReceiveWithAnyArgs()
            .GetPrimaryInstitutionForUserAsync(default, default);
    }

    [Theory]
    [InlineData("touch-n-go", "touch-n-go")]
    [InlineData("tng", "touch-n-go")]
    [InlineData("grabpay", "grabpay")]
    public async Task CreateMockWalletPayment_AcceptsSupportedWalletMethods(
        string submittedMethod,
        string storedMethod)
    {
        var institution = new Institution { Id = 9, Name = "Vega School" };
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.InstitutionRepository
            .GetPrimaryInstitutionForUserAsync(42, Arg.Any<CancellationToken>())
            .Returns(new InstitutionUser
            {
                InstitutionId = institution.Id,
                Institution = institution,
                UserId = 42,
                IsActive = true,
                IsPrimary = true,
            });
        var billingRepository = Substitute.For<IBillingRepository>();
        var handler = new CreateMockWalletPaymentCommandHandler(unitOfWork, billingRepository);

        var result = await handler.Handle(
            new CreateMockWalletPaymentCommand(42, submittedMethod),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(storedMethod, result.Result.PaymentMethod);
        Assert.Equal(BillingStatus.DemoSucceeded, result.Result.Status);
        await unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task CancelCheckoutSession_MarksOnlyResolvedInstitutionPendingTransactionAsCanceled()
    {
        var institution = new Institution { Id = 9, Name = "Vega School" };
        var transaction = new PaymentTransaction
        {
            InstitutionId = institution.Id,
            Provider = "stripe",
            PaymentMethod = "card",
            PlanId = "standard-monthly",
            Amount = 1990m,
            Currency = "MYR",
            Status = BillingStatus.Pending,
            StripeCheckoutSessionId = "cs_test_123",
        };
        var account = new BillingAccount
        {
            InstitutionId = institution.Id,
            Status = BillingStatus.Pending,
        };
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.InstitutionRepository
            .GetPrimaryInstitutionForUserAsync(42, Arg.Any<CancellationToken>())
            .Returns(new InstitutionUser
            {
                InstitutionId = institution.Id,
                Institution = institution,
                UserId = 42,
                IsActive = true,
                IsPrimary = true,
            });
        var billingRepository = Substitute.For<IBillingRepository>();
        billingRepository
            .GetPendingCheckoutTransactionAsync(
                institution.Id,
                "cs_test_123",
                Arg.Any<CancellationToken>())
            .Returns(transaction);
        billingRepository
            .GetBillingAccountAsync(institution.Id, true, Arg.Any<CancellationToken>())
            .Returns(account);
        var handler = new CancelCheckoutSessionCommandHandler(unitOfWork, billingRepository);

        var result = await handler.Handle(
            new CancelCheckoutSessionCommand(42, "cs_test_123"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BillingStatus.Canceled, result.Result.Status);
        Assert.Equal(BillingStatus.Canceled, transaction.Status);
        Assert.Equal(BillingStatus.Canceled, account.Status);
        await billingRepository.Received(1).GetPendingCheckoutTransactionAsync(
            institution.Id,
            "cs_test_123",
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync();
    }
}
