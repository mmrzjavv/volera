using MediatR;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class SendCompanyMessageCommandHandler : IRequestHandler<SendCompanyMessageCommand, Guid>
{
    private readonly ICompanyWidgetTokenService _widgetTokenService;
    private readonly IMessageRepository _messageRepository;
    private readonly ISystemLimitRepository _systemLimitRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public SendCompanyMessageCommandHandler(
        ICompanyWidgetTokenService widgetTokenService,
        IMessageRepository messageRepository,
        ISystemLimitRepository systemLimitRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        _widgetTokenService = widgetTokenService;
        _messageRepository = messageRepository;
        _systemLimitRepository = systemLimitRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(SendCompanyMessageCommand request, CancellationToken cancellationToken)
    {
        var client = await _widgetTokenService.ValidateCompanyClientTokenAsync(request.ClientToken, cancellationToken);
        if (client == null)
            throw new UnauthorizedAccessException("Invalid or expired client token.");

        var limitRecord = await _systemLimitRepository.GetByKeyAsync(LimitKeys.MaxGuestMessagesPerMinute, cancellationToken);
        var maxPerMinute = limitRecord != null ? (int)limitRecord.Value : 10;
        var since = DateTime.UtcNow.AddMinutes(-1);
        var count = await _messageRepository.GetCountBySenderSinceAsync(client.UserId, since, cancellationToken);
        if (count >= maxPerMinute)
            throw new InvalidOperationException($"Rate limit exceeded. Maximum {maxPerMinute} messages per minute.");

        var message = new Message(
            client.UserId,
            client.CompanyId,
            client.BranchId,
            request.Content ?? "",
            request.AttachmentUrl,
            request.AttachmentType,
            request.ReplyToMessageId);

        await _messageRepository.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        foreach (var domainEvent in message.DomainEvents)
            await _mediator.Publish(domainEvent, cancellationToken);
        message.ClearDomainEvents();

        return message.Id;
    }
}
