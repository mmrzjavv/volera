using Xunit;
using Moq;
using Core.Application.Handlers;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Tests.UnitTests;

public class SendMessageIdempotencyTests
{
    private static SendMessageCommandHandler CreateHandler(
        Mock<IMessageRepository> messageRepo,
        Mock<ISavedMessageRepository> savedRepo,
        Mock<IOutboxRepository> outboxRepo,
        Mock<IUnitOfWork> uow,
        Mock<ILimitResolutionService> limits)
    {
        var groupRepo = new Mock<IGroupRepository>();
        var userRepo = new Mock<IUserRepository>();
        return new SendMessageCommandHandler(
            messageRepo.Object,
            savedRepo.Object,
            outboxRepo.Object,
            groupRepo.Object,
            userRepo.Object,
            uow.Object,
            limits.Object);
    }

    [Fact]
    public async Task SendMessage_SameClientMessageId_ReturnsExistingWithoutSecondInsert()
    {
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var clientMessageId = Guid.NewGuid();
        var existing = new Message(senderId, receiverId, "hello");
        existing.AssignClientMessageId(clientMessageId);

        var messageRepo = new Mock<IMessageRepository>();
        messageRepo.Setup(r => r.GetBySenderAndClientMessageIdAsync(senderId, clientMessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var savedRepo = new Mock<ISavedMessageRepository>();
        var outboxRepo = new Mock<IOutboxRepository>();
        var uow = new Mock<IUnitOfWork>();
        var limits = new Mock<ILimitResolutionService>();

        var handler = CreateHandler(messageRepo, savedRepo, outboxRepo, uow, limits);

        var id = await handler.Handle(new SendMessageCommand
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = "hello",
            ClientMessageId = clientMessageId
        }, CancellationToken.None);

        Assert.Equal(existing.Id, id);
        messageRepo.Verify(r => r.AddAsync(It.IsAny<Message>()), Times.Never);
        outboxRepo.Verify(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SendMessage_NewClientMessageId_WritesMessageAndOutbox()
    {
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var clientMessageId = Guid.NewGuid();

        var messageRepo = new Mock<IMessageRepository>();
        messageRepo.Setup(r => r.GetBySenderAndClientMessageIdAsync(senderId, clientMessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message?)null);
        messageRepo.Setup(r => r.AddAsync(It.IsAny<Message>())).Returns(Task.CompletedTask);

        var savedRepo = new Mock<ISavedMessageRepository>();
        var outboxRepo = new Mock<IOutboxRepository>();
        outboxRepo.Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        var limits = new Mock<ILimitResolutionService>();
        limits.Setup(l => l.GetEffectiveLimitAsync(senderId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler(messageRepo, savedRepo, outboxRepo, uow, limits);

        var id = await handler.Handle(new SendMessageCommand
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = "hello",
            ClientMessageId = clientMessageId
        }, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        messageRepo.Verify(r => r.AddAsync(It.Is<Message>(m => m.ClientMessageId == clientMessageId)), Times.Once);
        outboxRepo.Verify(r => r.AddAsync(It.Is<OutboxMessage>(o => o.Type == SendMessageCommandHandler.OutboxTypeMessageSent), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
