using Xunit;
using Moq;
using Core.Application.Handlers;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Tests.UnitTests;

public class CompanyLoginCommandHandlerTests
{
    [Fact]
    public async Task DemoOtp_Rejected_WhenNotAllowed()
    {
        var company = new Company("Acme", "+989121111111");
        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByMobileNumberAsync("+989121111111", It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);

        var tokenService = new Mock<ICompanyTokenService>();
        tokenService.Setup(t => t.ValidateTokenAsync("1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        var handler = new CompanyLoginCommandHandler(
            companyRepo.Object,
            tokenService.Object,
            Mock.Of<IRefreshTokenHasher>(),
            Mock.Of<IUnitOfWork>());

        var result = await handler.Handle(new CompanyLoginCommand
        {
            MobileNumber = "+989121111111",
            Token = "1234",
            AllowDemoOtp = false,
            DemoOtpValue = "1234"
        }, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DemoOtp_Accepted_WhenExplicitlyAllowed()
    {
        var company = new Company("Acme", "+989121111111");
        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByMobileNumberAsync("+989121111111", It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        companyRepo.Setup(r => r.Update(It.IsAny<Company>()));

        var tokenService = new Mock<ICompanyTokenService>();
        tokenService.Setup(t => t.GenerateSecureToken()).Returns("new-session-token");

        var hasher = new Mock<IRefreshTokenHasher>();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var handler = new CompanyLoginCommandHandler(
            companyRepo.Object,
            tokenService.Object,
            hasher.Object,
            uow.Object);

        var result = await handler.Handle(new CompanyLoginCommand
        {
            MobileNumber = "+989121111111",
            Token = "9999",
            AllowDemoOtp = true,
            DemoOtpValue = "9999"
        }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("new-session-token", result!.Token);
    }
}
