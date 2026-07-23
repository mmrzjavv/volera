using Xunit;
using Moq;
using Core.Application.Handlers;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Interfaces;

namespace Tests.UnitTests;

public class ChannelTests
{
    [Fact]
    public async Task CreateChannel_OwnerCanPost_SubscriberCannot()
    {
        var owner = new User("O", "W", "owner", "09120000000", "pass");
        var subscriberId = Guid.NewGuid();
        Group? saved = null;

        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.IsPublicUsernameTakenAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        groupRepo.Setup(r => r.AddAsync(It.IsAny<Group>())).Callback<Group>(g => saved = g).Returns(Task.CompletedTask);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(owner.Id)).ReturnsAsync(owner);
        userRepo.Setup(r => r.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var handler = new CreateChannelCommandHandler(groupRepo.Object, userRepo.Object, uow.Object);
        var channelId = await handler.Handle(new CreateChannelCommand
        {
            Name = "News",
            CreatorId = owner.Id,
            IsPublic = false
        }, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, channelId);
        Assert.NotNull(saved);
        Assert.Equal(GroupKind.Channel, saved!.Kind);
        Assert.True(saved.CanUserPost(owner.Id));

        saved.AddMember(subscriberId, false);
        Assert.False(saved.CanUserPost(subscriberId));
    }

    [Fact]
    public async Task SendMessage_Channel_SubscriberThrowsUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var subscriberId = Guid.NewGuid();
        var channel = Group.CreateChannel("News", ownerId, null, false, null);
        channel.AddMember(subscriberId, false);

        var messageRepo = new Mock<IMessageRepository>();
        var savedRepo = new Mock<ISavedMessageRepository>();
        var outboxRepo = new Mock<IOutboxRepository>();
        var groupRepo = new Mock<IGroupRepository>();
        groupRepo.Setup(r => r.GetGroupWithMembersAsync(channel.Id)).ReturnsAsync(channel);
        var userRepo = new Mock<IUserRepository>();
        var uow = new Mock<IUnitOfWork>();
        var limits = new Mock<ILimitResolutionService>();
        limits.Setup(l => l.GetEffectiveLimitAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = new SendMessageCommandHandler(
            messageRepo.Object, savedRepo.Object, outboxRepo.Object, groupRepo.Object, userRepo.Object, uow.Object, limits.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new SendMessageCommand
        {
            SenderId = subscriberId,
            GroupId = channel.Id,
            Content = "hi"
        }, CancellationToken.None));
    }
}
