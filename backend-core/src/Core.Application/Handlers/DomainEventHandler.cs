using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Core.Domain.Events;
using Core.Application.Interfaces;

namespace Core.Application.Handlers;

public class DomainEventHandler :
    INotificationHandler<CallInitiatedEvent>,
    INotificationHandler<CallAcceptedEvent>,
    INotificationHandler<CallRejectedEvent>,
    INotificationHandler<CallEndedEvent>,
    INotificationHandler<MissedCallEvent>,
    INotificationHandler<MessageSentEvent>,
    INotificationHandler<MessageEditedEvent>,
    INotificationHandler<MessageDeletedEvent>,
    INotificationHandler<GroupCallInitiatedEvent>,
    INotificationHandler<GroupCallJoinedEvent>,
    INotificationHandler<GroupCallLeftEvent>,
    INotificationHandler<GroupCallEndedEvent>
{
    private readonly ICallNotificationService _callNotificationService;
    private readonly IMessageNotificationService _messageNotificationService;
    private readonly IGroupCallNotificationService _groupCallNotificationService;

    public DomainEventHandler(
        ICallNotificationService callNotificationService,
        IMessageNotificationService messageNotificationService,
        IGroupCallNotificationService groupCallNotificationService)
    {
        _callNotificationService = callNotificationService;
        _messageNotificationService = messageNotificationService;
        _groupCallNotificationService = groupCallNotificationService;
    }

    public async Task Handle(CallInitiatedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DomainEventHandler] CallInitiatedEvent received - CallId: {notification.CallId}, CallerId: {notification.CallerId}, ReceiverId: {notification.ReceiverId}");
        await _callNotificationService.SendCallInitiated(notification.CallId.ToString(), notification.CallerId, notification.ReceiverId, notification.IsVideo);
        Console.WriteLine($"[DomainEventHandler] SendCallInitiated completed");
    }

    public async Task Handle(CallAcceptedEvent notification, CancellationToken cancellationToken)
    {
        await _callNotificationService.SendCallAccepted(notification.CallId.ToString(), notification.CallerId, notification.ReceiverId);
    }

    public async Task Handle(CallRejectedEvent notification, CancellationToken cancellationToken)
    {
        await _callNotificationService.SendCallRejected(notification.CallId.ToString(), notification.CallerId, notification.ReceiverId);
    }

    public async Task Handle(CallEndedEvent notification, CancellationToken cancellationToken)
    {
        await _callNotificationService.SendCallEnded(notification.CallId.ToString(), notification.CallerId, notification.ReceiverId, notification.Duration);
    }

    public async Task Handle(MissedCallEvent notification, CancellationToken cancellationToken)
    {
        await _callNotificationService.SendMissedCall(notification.CallId.ToString(), notification.CallerId, notification.ReceiverId);
    }

    public async Task Handle(MessageSentEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DomainEventHandler] MessageSentEvent received - MessageId: {notification.MessageId}, SenderId: {notification.SenderId}, ReceiverId: {notification.ReceiverId}, GroupId: {notification.GroupId}");
        await _messageNotificationService.SendMessage(notification.MessageId, notification.SenderId, notification.ReceiverId, notification.GroupId, notification.Content, notification.SentAt, notification.AttachmentUrl, notification.AttachmentType, notification.BranchId, notification.ReplyToMessageId, notification.SupportSenderId);
    }

    public async Task Handle(MessageEditedEvent notification, CancellationToken cancellationToken)
    {
        await _messageNotificationService.NotifyMessageEdited(notification.MessageId, notification.SenderId, notification.ReceiverId, notification.GroupId, notification.NewContent, notification.EditedAt);
    }

    public async Task Handle(MessageDeletedEvent notification, CancellationToken cancellationToken)
    {
        await _messageNotificationService.NotifyMessageDeleted(notification.MessageId, notification.SenderId, notification.ReceiverId, notification.GroupId, notification.DeletedAt);
    }

    public Task Handle(GroupCallInitiatedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DomainEventHandler] GroupCallInitiatedEvent received - GroupCallId: {notification.GroupCallId}, GroupId: {notification.GroupId}, InitiatorId: {notification.InitiatorId}");
        // The list of member user ids will be resolved in the command handler and passed via notification service
        // so here we only log; notifications are triggered from the command handler.
        return Task.CompletedTask;
    }

    public Task Handle(GroupCallJoinedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DomainEventHandler] GroupCallJoinedEvent received - GroupCallId: {notification.GroupCallId}, UserId: {notification.UserId}");
        // Participant notifications are also handled in the command handlers with richer context (user name, etc.).
        return Task.CompletedTask;
    }

    public Task Handle(GroupCallLeftEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DomainEventHandler] GroupCallLeftEvent received - GroupCallId: {notification.GroupCallId}, UserId: {notification.UserId}");
        return Task.CompletedTask;
    }

    public Task Handle(GroupCallEndedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DomainEventHandler] GroupCallEndedEvent received - GroupCallId: {notification.GroupCallId}, GroupId: {notification.GroupId}, EndedByUserId: {notification.EndedByUserId}");
        return Task.CompletedTask;
    }
}