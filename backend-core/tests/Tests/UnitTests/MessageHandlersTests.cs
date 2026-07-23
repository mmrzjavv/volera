using Xunit;
using Moq;
using Core.Application.Handlers;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Core.Application.Commands;

namespace Tests.UnitTests;

public class MessageHandlersTests
{
    [Fact]
    public async Task EditMessage_ValidRequest_EditsMessageAndWritesOutbox()
    {
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var message = new Message(senderId, receiverId, "Original content");

        var messageRepositoryMock = new Mock<IMessageRepository>();
        messageRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(message);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var outboxMock = new Mock<IOutboxRepository>();
        outboxMock.Setup(o => o.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new EditMessageCommandHandler(messageRepositoryMock.Object, outboxMock.Object, unitOfWorkMock.Object);

        var result = await handler.Handle(new EditMessageCommand
        {
            MessageId = message.Id,
            UserId = senderId,
            NewContent = "Edited content"
        }, CancellationToken.None);

        Assert.True(result);
        Assert.Equal("Edited content", message.Content);
        Assert.True(message.IsEdited);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        outboxMock.Verify(o => o.AddAsync(
            It.Is<OutboxMessage>(m => m.Type == EditMessageCommandHandler.OutboxTypeMessageEdited),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMessage_ValidRequest_DeletesMessageAndWritesOutbox()
    {
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var message = new Message(senderId, receiverId, "Content");

        var messageRepositoryMock = new Mock<IMessageRepository>();
        messageRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(message);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var outboxMock = new Mock<IOutboxRepository>();
        outboxMock.Setup(o => o.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new DeleteMessageCommandHandler(messageRepositoryMock.Object, outboxMock.Object, unitOfWorkMock.Object);

        var result = await handler.Handle(new DeleteMessageCommand
        {
            MessageId = message.Id,
            UserId = senderId
        }, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(message.DeletedAt);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        outboxMock.Verify(o => o.AddAsync(
            It.Is<OutboxMessage>(m => m.Type == DeleteMessageCommandHandler.OutboxTypeMessageDeleted),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
