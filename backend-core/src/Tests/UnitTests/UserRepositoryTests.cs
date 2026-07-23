using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace UnitTests.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new UserRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetUsersAsync_ShouldReturnPaginatedAndFilteredUsers()
        {
            // Arrange
            var users = new List<User>();
            for (int i = 0; i < 30; i++)
            {
                var user = new User(
                    firstName: $"User{i}", 
                    lastName: "Test", 
                    username: $"user{i}", 
                    phoneNumber: $"12345{i}", 
                    passwordHash: "hash"
                );
                // Reflection to ensure predictable ordering if needed, but OrderBy FirstName is used.
                // User0, User1, User10, User11... User2, User20...
                // Wait, strings sort differently.
                // User0
                // User1
                // User10
                // ...
                // User19
                // User2
                // User20
                // ...
                users.Add(user);
            }
            // Add a specific user for search
            var searchTarget = new User("Alice", "Wonderland", "alice", "999999", "hash");
            users.Add(searchTarget);

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act - Fetch Page 1, Size 10, No term
            var (items1, total1) = await _repository.GetUsersAsync(1, 10, null, null);

            // Assert
            total1.Should().Be(31);
            items1.Should().HaveCount(10);
            // Alice should be first (A)
            items1.First().FirstName.Should().Be("Alice");

            // Act - Fetch Page 2
            var (items2, total2) = await _repository.GetUsersAsync(2, 10, null, null);
            items2.Should().HaveCount(10);
            items2.Should().NotContain(u => u.FirstName == "Alice");

            // Act - Search for "Alice"
            var (itemsSearch, totalSearch) = await _repository.GetUsersAsync(1, 10, "alice", null);
            totalSearch.Should().Be(1);
            itemsSearch.Should().HaveCount(1);
            itemsSearch.First().FirstName.Should().Be("Alice");

            // Act - Search for "User1" (Matches User1, User10..User19) -> 11 users
            var (itemsSearch2, totalSearch2) = await _repository.GetUsersAsync(1, 10, "user1", null);
            totalSearch2.Should().Be(11); // User1, User10, User11...User19
            itemsSearch2.Should().HaveCount(10);
        }
    }
}
