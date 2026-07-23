using Xunit;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Core.Domain.Entities;

namespace Tests.IntegrationTests;

public class UserRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task AddUser_ShouldPersistUser()
    {
        // Arrange
        var user = new User("John", "Doe", "johndoe", "1234567890", "hashedpassword");

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("johndoe", savedUser.Username);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}