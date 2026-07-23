using Core.Application.DTOs;
using Core.Application.Queries;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class GetUnreadCountsQueryHandler : IRequestHandler<GetUnreadCountsQuery, IEnumerable<UnreadCountDto>>
{
    private readonly IMessageRepository _messageRepository;

    public GetUnreadCountsQueryHandler(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<IEnumerable<UnreadCountDto>> Handle(GetUnreadCountsQuery request, CancellationToken cancellationToken)
    {
        var counts = await _messageRepository.GetUnreadCountsAsync(request.UserId);

        return counts.Select(kvp => new UnreadCountDto
        {
            SenderId = kvp.Key,
            Count = kvp.Value
        });
    }
}
