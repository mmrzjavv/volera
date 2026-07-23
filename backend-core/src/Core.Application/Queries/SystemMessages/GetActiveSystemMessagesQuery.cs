using Core.Application.DTOs;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Queries.SystemMessages;

public record GetActiveSystemMessagesQuery(Guid UserId) : IRequest<IEnumerable<SystemMessageDto>>;

public class GetActiveSystemMessagesQueryHandler : IRequestHandler<GetActiveSystemMessagesQuery, IEnumerable<SystemMessageDto>>
{
    private readonly ISystemMessageRepository _systemMessageRepository;
    private readonly ISystemMessageReadRepository _systemMessageReadRepository;

    public GetActiveSystemMessagesQueryHandler(
        ISystemMessageRepository systemMessageRepository,
        ISystemMessageReadRepository systemMessageReadRepository)
    {
        _systemMessageRepository = systemMessageRepository;
        _systemMessageReadRepository = systemMessageReadRepository;
    }

    public async Task<IEnumerable<SystemMessageDto>> Handle(GetActiveSystemMessagesQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var messages = await _systemMessageRepository.GetActiveAsync(now, cancellationToken);
        if (messages.Count == 0)
        {
            return Enumerable.Empty<SystemMessageDto>();
        }

        var messageIds = messages.Select(m => m.Id).ToArray();
        var readIds = await _systemMessageReadRepository.GetReadMessageIdsForUserAsync(request.UserId, messageIds, cancellationToken);

        return messages.Select(m =>
            new SystemMessageDto(
                m.Id,
                m.Title,
                m.Content,
                m.CreatedAt,
                m.ExpiresAt,
                m.IsActive,
                readIds.Contains(m.Id)));
    }
}

