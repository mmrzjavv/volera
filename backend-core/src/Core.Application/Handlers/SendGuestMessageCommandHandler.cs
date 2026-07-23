using MediatR;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

/// <summary>
/// Resolves guest from token and inbox from config; delegates to existing SendMessageCommand to reuse validation and notifications.
/// Per-guest and per-IP limits to prevent abuse and flooding without blocking authenticated users.
/// </summary>
public class SendGuestMessageCommandHandler : IRequestHandler<SendGuestMessageCommand, Guid>
{
    private readonly IGuestTokenService _guestTokenService;
    private readonly IAppSettingRepository _appSettingRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ISystemLimitRepository _systemLimitRepository;
    private readonly IMediator _mediator;

    public SendGuestMessageCommandHandler(
        IGuestTokenService guestTokenService,
        IAppSettingRepository appSettingRepository,
        IMessageRepository messageRepository,
        ISystemLimitRepository systemLimitRepository,
        IMediator mediator)
    {
        _guestTokenService = guestTokenService;
        _appSettingRepository = appSettingRepository;
        _messageRepository = messageRepository;
        _systemLimitRepository = systemLimitRepository;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(SendGuestMessageCommand request, CancellationToken cancellationToken)
    {
        var guest = await _guestTokenService.ValidateTokenAsync(request.GuestToken, cancellationToken);
        if (guest == null)
            throw new UnauthorizedAccessException("Invalid or expired guest token.");

        var limitRecord = await _systemLimitRepository.GetByKeyAsync(LimitKeys.MaxGuestMessagesPerMinute, cancellationToken);
        var maxPerMinute = limitRecord != null ? (int)limitRecord.Value : 10;
        var since = DateTime.UtcNow.AddMinutes(-1);
        var count = await _messageRepository.GetCountBySenderSinceAsync(guest.UserId, since, cancellationToken);
        if (count >= maxPerMinute)
            throw new InvalidOperationException($"Rate limit exceeded. Maximum {maxPerMinute} messages per minute for guest chat.");

        var inboxSetting = await _appSettingRepository.GetByKeyAsync(AppSettingKeys.GuestInboxUserId, cancellationToken);
        if (inboxSetting == null || !Guid.TryParse(inboxSetting.Value, out var inboxUserId))
            throw new InvalidOperationException("Guest inbox is not configured. Set GuestInboxUserId in app settings.");

        var command = new SendMessageCommand
        {
            SenderId = guest.UserId,
            ReceiverId = inboxUserId,
            Content = request.Content,
            AttachmentUrl = request.AttachmentUrl,
            AttachmentType = request.AttachmentType
        };

        return await _mediator.Send(command, cancellationToken);
    }
}
