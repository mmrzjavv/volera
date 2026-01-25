using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Core.Domain.Events;
using Core.Application.Interfaces;

namespace Core.Application.Handlers;

public class DomainEventHandler :
    INotificationHandler<CallInitiatedEvent>,
    INotificationHandler<CallAcceptedEvent>,
    INotificationHandler<CallEndedEvent>,
    INotificationHandler<MissedCallEvent>
{
    private readonly ICallNotificationService _notificationService;

    public DomainEventHandler(ICallNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Handle(CallInitiatedEvent notification, CancellationToken cancellationToken)
    {
        await _notificationService.SendCallInitiated(notification.CallId.ToString(), notification.CallerId, notification.ReceiverId);
    }

    public async Task Handle(CallAcceptedEvent notification, CancellationToken cancellationToken)
    {
        await _notificationService.SendCallAccepted(notification.CallId.ToString(), notification.CallerId, notification.ReceiverId);
    }

    public async Task Handle(CallEndedEvent notification, CancellationToken cancellationToken)
    {
        await _notificationService.SendCallEnded(notification.CallId.ToString(), notification.CallerId, notification.ReceiverId, notification.Duration);
    }

    public async Task Handle(MissedCallEvent notification, CancellationToken cancellationToken)
    {
        await _notificationService.SendMissedCall(notification.CallId.ToString(), notification.CallerId, notification.ReceiverId);
    }
}