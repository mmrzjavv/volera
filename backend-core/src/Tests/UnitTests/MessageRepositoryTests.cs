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
    public class MessageRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly MessageRepository _repository;

        public MessageRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new MessageRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetConversationAsync_ShouldReturnPaginatedMessages()
        {
            // Arrange
            var senderId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            var sender = new User("Sender", "User", "sender", "1234567890", "hash");
            // Reflection to set ID for test
            SetProperty(sender, "Id", senderId);
            var receiver = new User("Receiver", "User", "receiver", "0987654321", "hash");
            SetProperty(receiver, "Id", receiverId);

            _context.Users.AddRange(sender, receiver);

            var messages = new List<Message>();
            for (int i = 0; i < 50; i++)
            {
                var msg = new Message(senderId, receiverId, $"Message {i}", null, null);
                // Set SentAt to be different for each
                SetProperty(msg, "SentAt", DateTime.UtcNow.AddMinutes(-i)); // 0 is newest
                messages.Add(msg);
            }
            _context.Messages.AddRange(messages);
            await _context.SaveChangesAsync();

            // Act - Fetch first page (newest 20)
            var result1 = await _repository.GetConversationAsync(senderId, receiverId, 20, null);

            // Assert
            result1.Should().HaveCount(20);

            // Implementation returns messages ordered by SentAt (ASC) (Oldest -> Newest)
            // Page 1 (no before) gets top 20 newest messages (Message 0 to Message 19).
            // Reordered ASC: Message 19 to Message 0.

            result1.Last().Content.Should().Be("Message 0");
            result1.First().Content.Should().Be("Message 19");

            // Act - Fetch next page
            var oldestInPage1 = result1.First();
            var result2 = await _repository.GetConversationAsync(senderId, receiverId, 20, oldestInPage1.SentAt);

            // Assert
            result2.Should().HaveCount(20);
            // Should be Message 20 to Message 39
            result2.Last().Content.Should().Be("Message 20");
            result2.First().Content.Should().Be("Message 39");
        }

        [Fact]
        public async Task GetGroupMessagesAsync_ShouldReturnPaginatedMessages()
        {
            // Arrange
            var senderId = Guid.NewGuid();
            var groupId = Guid.NewGuid();

            // Create a group member relation if repository enforces it?
            // Repository implementation:
            // var query = _dbSet.Where(m => m.GroupId == groupId);
            // It does NOT check membership. The Handler checks membership.
            // So for Repository test, we just need messages with GroupId.

            var messages = new List<Message>();
            for (int i = 0; i < 50; i++)
            {
                // Group Message Constructor:
                // public Message(Guid senderId, Guid groupId, string content, bool isGroupMessage, ...)
                var msg = new Message(senderId, groupId, $"Group Message {i}", true, null, null);
                SetProperty(msg, "SentAt", DateTime.UtcNow.AddMinutes(-i)); // 0 is newest
                messages.Add(msg);
            }
            _context.Messages.AddRange(messages);
            await _context.SaveChangesAsync();

            // Act - Fetch first page
            var result1 = await _repository.GetGroupMessagesAsync(groupId, 20, null);

            // Assert
            result1.Should().HaveCount(20);
            result1.Last().Content.Should().Be("Group Message 0");
            result1.First().Content.Should().Be("Group Message 19");

            // Act - Fetch next page
            var oldestInPage1 = result1.First();
            var result2 = await _repository.GetGroupMessagesAsync(groupId, 20, oldestInPage1.SentAt);

            // Assert
            result2.Should().HaveCount(20);
            result2.Last().Content.Should().Be("Group Message 20");
            result2.First().Content.Should().Be("Group Message 39");
        }

        [Fact]
        public async Task GetUnreadCountsAsync_ShouldReturnCorrectCounts()
        {
            // Arrange
            var receiverId = Guid.NewGuid();
            var sender1 = Guid.NewGuid();
            var sender2 = Guid.NewGuid();

            var messages = new List<Message>();

            // Sender 1: 3 unread, 2 read
            for (int i = 0; i < 5; i++)
            {
                var msg = new Message(sender1, receiverId, $"Msg {i}");
                if (i >= 3) msg.MarkAsRead(); // 3 and 4 are read. 0, 1, 2 are unread.
                messages.Add(msg);
            }

            // Sender 2: 1 unread
            var msg2 = new Message(sender2, receiverId, "Msg S2");
            messages.Add(msg2);

            _context.Messages.AddRange(messages);
            await _context.SaveChangesAsync();

            // Act
            var counts = await _repository.GetUnreadCountsAsync(receiverId);

            // Assert
            counts.Should().HaveCount(2);
            counts[sender1].Should().Be(3);
            counts[sender2].Should().Be(1);
        }

        private void SetProperty(object obj, string propName, object value)
        {
            var prop = obj.GetType().GetProperty(propName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
            }
            else
            {
                // Try backing field if property is read-only
                var field = obj.GetType().GetField($"<{propName}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(obj, value);
                }
            }
        }
    }
}
