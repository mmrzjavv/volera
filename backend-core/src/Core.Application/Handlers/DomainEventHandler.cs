using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Core.Domain.Events;
using Core.Application.Interfaces;
using Core.Application.Logging;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<DomainEventHandler> _logger;

    public DomainEventHandler(
        ICallNotificationService callNotificationService,
        IMessageNotificationService messageNotificationService,
        ILogger<DomainEventHandler> logger)
    {
        _callNotificationService = callNotificationService;
        _messageNotificationService = messageNotificationService;
        _logger = logger;
    }

    public async Task Handle(CallInitiatedEvent notification, CancellationToken cancellationToken)
    {
        await _callNotificationService.SendCallInitiated(
            notification.CallId.ToString(),
            notification.CallerId,
            notification.ReceiverId,
            notification.IsVideo);
    }

    public async Task Handle(CallAcceptedEvent notification, CancellationToken cancellationToken)
    {
        await _callNotificationService.SendCallAccepted(
            notification.CallId.ToString(),
            notification.CallerId,
            notification.ReceiverId);
    }

    public async Task Handle(CallRejectedEvent notification, CancellationToken cancellationToken)
    {
        await _callNotificationService.SendCallRejected(
            notification.CallId.ToString(),
            notification.CallerId,
            notification.ReceiverId);
    }

    public async Task Handle(CallEndedEvent notification, CancellationToken cancellationToken)
    {
        await _callNotificationService.SendCallEnded(
            notification.CallId.ToString(),
            notification.CallerId,
            notification.ReceiverId,
            notification.Duration);
    }

    public async Task Handle(MissedCallEvent notification, CancellationToken cancellationToken)
    {
        await _callNotificationService.SendMissedCall(
            notification.CallId.ToString(),
            notification.CallerId,
            notification.ReceiverId);
    }

    public async Task Handle(MessageSentEvent notification, CancellationToken cancellationToken)
    {
        await _messageNotificationService.SendMessage(
            notification.MessageId,
            notification.SenderId,
            notification.ReceiverId,
            notification.GroupId,
            notification.Content,
            notification.SentAt,
            notification.AttachmentUrl,
            notification.AttachmentType,
            notification.BranchId,
            notification.ReplyToMessageId,
            notification.SupportSenderId);
    }

    public async Task Handle(MessageEditedEvent notification, CancellationToken cancellationToken)
    {
        await _messageNotificationService.NotifyMessageEdited(
            notification.MessageId,
            notification.SenderId,
            notification.ReceiverId,
            notification.GroupId,
            notification.NewContent,
            notification.EditedAt);
    }

    public async Task Handle(MessageDeletedEvent notification, CancellationToken cancellationToken)
    {
        await _messageNotificationService.NotifyMessageDeleted(
            notification.MessageId,
            notification.SenderId,
            notification.ReceiverId,
            notification.GroupId,
            notification.DeletedAt);
    }

    public Task Handle(GroupCallInitiatedEvent notification, CancellationToken cancellationToken)
    {
        AppLog.Info(_logger, AppLogEvents.GroupCallInitiated,
            "GroupCallId: {GroupCallId} | GroupId: {GroupId} | InitiatorId: {InitiatorId} | Result: Success",
            notification.GroupCallId, notification.GroupId, notification.InitiatorId);
        return Task.CompletedTask;
    }

    public Task Handle(GroupCallJoinedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "GroupCallJoined | GroupCallId: {GroupCallId} | UserId: {UserId}",
            notification.GroupCallId, notification.UserId);
        return Task.CompletedTask;
    }

    public Task Handle(GroupCallLeftEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "GroupCallLeft | GroupCallId: {GroupCallId} | UserId: {UserId}",
            notification.GroupCallId, notification.UserId);
        return Task.CompletedTask;
    }

    public Task Handle(GroupCallEndedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "GroupCallEnded | GroupCallId: {GroupCallId} | GroupId: {GroupId} | EndedByUserId: {EndedByUserId}",
            notification.GroupCallId, notification.GroupId, notification.EndedByUserId);
        return Task.CompletedTask;
    }
}
