using MediatR;
using Core.Application.Commands;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class ForwardMessageCommandHandler : IRequestHandler<ForwardMessageCommand, Guid>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public ForwardMessageCommandHandler(
        IMessageRepository messageRepository,
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        _messageRepository = messageRepository;
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(ForwardMessageCommand request, CancellationToken cancellationToken)
    {
        var sourceMessage = await _messageRepository.GetByIdAsync(request.MessageId);
        if (sourceMessage == null)
            throw new KeyNotFoundException("Source message not found.");

        // Authorization: user must be part of the source conversation
        if (sourceMessage.GroupId.HasValue)
        {
            var group = await _groupRepository.GetGroupWithMembersAsync(sourceMessage.GroupId.Value);
            if (group == null || !group.Members.Any(m => m.UserId == request.UserId))
                throw new UnauthorizedAccessException("You are not a member of the source group.");
        }
        else
        {
            if (sourceMessage.SenderId != request.UserId && sourceMessage.ReceiverId != request.UserId)
                throw new UnauthorizedAccessException("You are not a participant in the source conversation.");
        }

        var forwardedAt = DateTime.UtcNow;
        Message newMessage;

        if (request.GroupId.HasValue)
        {
            // Target is group
            var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId.Value);
            if (group == null || !group.Members.Any(m => m.UserId == request.UserId))
                throw new UnauthorizedAccessException("You are not a member of the target group.");

            newMessage = new Message(
                request.UserId,
                request.GroupId.Value,
                sourceMessage.Content,
                true,
                sourceMessage.AttachmentUrl,
                sourceMessage.AttachmentType,
                null,
                sourceMessage.Id,
                forwardedAt);
        }
        else
        {
            // Target is direct user
            newMessage = new Message(
                request.UserId,
                request.ReceiverId!.Value,
                sourceMessage.Content,
                sourceMessage.AttachmentUrl,
                sourceMessage.AttachmentType,
                null,
                sourceMessage.Id,
                forwardedAt);
        }

        await _messageRepository.AddAsync(newMessage);
        await _unitOfWork.SaveChangesAsync();

        // Publish domain events so SignalR notifications fire
        foreach (var domainEvent in newMessage.DomainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        newMessage.ClearDomainEvents();

        return newMessage.Id;
    }
}

