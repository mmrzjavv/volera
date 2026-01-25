using Xunit;
using Moq;
using Core.Application.Handlers;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;

namespace Tests.UnitTests;

public class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_ReturnsUserId()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock.Setup(r => r.IsUsernameUniqueAsync("testuser")).ReturnsAsync(true);
        userRepositoryMock.Setup(r => r.IsPhoneNumberUniqueAsync("1234567890")).ReturnsAsync(true);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        passwordHasherMock.Setup(h => h.HashPassword("password")).Returns("hashed");

        var handler = new RegisterUserCommandHandler(userRepositoryMock.Object, unitOfWorkMock.Object, passwordHasherMock.Object);

        var command = new Core.Application.Commands.RegisterUserCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Username = "testuser",
            PhoneNumber = "1234567890",
            Password = "password"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}