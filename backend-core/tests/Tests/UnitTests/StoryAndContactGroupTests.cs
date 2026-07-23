using Xunit;
using FluentValidation.TestHelper;
using Core.Application.Commands;
using Core.Application.DTOs;
using Core.Application.Validators;
using Core.Application.Handlers;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Moq;

namespace Tests.UnitTests;

public class StoryAndContactGroupTests
{
    [Fact]
    public void CreateStoryValidator_RejectsEmptyItems()
    {
        var validator = new CreateStoryCommandValidator();
        var result = validator.TestValidate(new CreateStoryCommand
        {
            UserId = Guid.NewGuid(),
            Items = new List<CreateStoryItemDto>()
        });
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void CreateStoryValidator_AcceptsValidImageItem()
    {
        var validator = new CreateStoryCommandValidator();
        var result = validator.TestValidate(new CreateStoryCommand
        {
            UserId = Guid.NewGuid(),
            Items = new List<CreateStoryItemDto>
            {
                new() { ObjectKey = "stories/a.jpg", MediaType = "Image", DurationMs = 5000 }
            }
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateGroup_ThrowsWhenMemberMissing()
    {
        var groupRepo = new Mock<IGroupRepository>();
        var userRepo = new Mock<IUserRepository>();
        var uow = new Mock<IUnitOfWork>();
        var missingId = Guid.NewGuid();
        userRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((User?)null);

        var handler = new CreateGroupCommandHandler(groupRepo.Object, userRepo.Object, uow.Object);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(new CreateGroupCommand
        {
            Name = "Test",
            CreatorId = Guid.NewGuid(),
            MemberIds = new List<Guid> { missingId }
        }, CancellationToken.None));
    }

    [Fact]
    public async Task AddContact_UsesProvidedContactName()
    {
        var contactRepo = new Mock<IContactRepository>();
        var userRepo = new Mock<IUserRepository>();
        var uow = new Mock<IUnitOfWork>();

        var contactUser = new User("A", "B", "ab", "09120000000", "hash");
        userRepo.Setup(r => r.GetByUsernameAsync("09120000000")).ReturnsAsync((User?)null);
        userRepo.Setup(r => r.GetByPhoneNumberAsync("09120000000")).ReturnsAsync(contactUser);
        contactRepo.Setup(r => r.ContactExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

        Contact? saved = null;
        contactRepo.Setup(r => r.AddAsync(It.IsAny<Contact>()))
            .Callback<Contact>(c => saved = c)
            .Returns(Task.CompletedTask);

        var ownerId = Guid.NewGuid();
        var handler = new AddContactCommandHandler(contactRepo.Object, userRepo.Object, uow.Object);
        await handler.Handle(new AddContactCommand
        {
            OwnerUserId = ownerId,
            ContactIdentifier = "09120000000",
            ContactName = "Mom"
        }, CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal("Mom", saved!.ContactName);
    }

    [Fact]
    public async Task AddContact_ResolvesUserByUsername()
    {
        var contactRepo = new Mock<IContactRepository>();
        var userRepo = new Mock<IUserRepository>();
        var uow = new Mock<IUnitOfWork>();

        var contactUser = new User("Jane", "Doe", "janedoe", "+989121111111", "hash");
        userRepo.Setup(r => r.GetByUsernameAsync("janedoe")).ReturnsAsync(contactUser);
        contactRepo.Setup(r => r.ContactExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

        Contact? saved = null;
        contactRepo.Setup(r => r.AddAsync(It.IsAny<Contact>()))
            .Callback<Contact>(c => saved = c)
            .Returns(Task.CompletedTask);

        var ownerId = Guid.NewGuid();
        var handler = new AddContactCommandHandler(contactRepo.Object, userRepo.Object, uow.Object);
        await handler.Handle(new AddContactCommand
        {
            OwnerUserId = ownerId,
            ContactIdentifier = "janedoe",
            ContactName = "Jane"
        }, CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal("Jane", saved!.ContactName);
        Assert.Equal(contactUser.Id, saved.ContactUserId);
        Assert.Equal("+989121111111", saved.ContactPhoneNumber);
        userRepo.Verify(r => r.GetByPhoneNumberAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddContact_ThrowsWhenUsernameNotFound()
    {
        var contactRepo = new Mock<IContactRepository>();
        var userRepo = new Mock<IUserRepository>();
        var uow = new Mock<IUnitOfWork>();

        userRepo.Setup(r => r.GetByUsernameAsync("nobody")).ReturnsAsync((User?)null);
        userRepo.Setup(r => r.GetByPhoneNumberAsync("nobody")).ReturnsAsync((User?)null);

        var handler = new AddContactCommandHandler(contactRepo.Object, userRepo.Object, uow.Object);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(new AddContactCommand
        {
            OwnerUserId = Guid.NewGuid(),
            ContactIdentifier = "nobody",
            ContactName = "Ghost"
        }, CancellationToken.None));
    }
}
