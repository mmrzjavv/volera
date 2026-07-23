using MediatR;
using Core.Domain.Interfaces;
using Core.Application.Queries;

namespace Core.Application.Handlers;

public class GetTotalMessagesCountQueryHandler : IRequestHandler<GetTotalMessagesCountQuery, int>
{
    private readonly IMessageRepository _messageRepository;

    public GetTotalMessagesCountQueryHandler(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<int> Handle(GetTotalMessagesCountQuery request, CancellationToken cancellationToken)
    {
        return await _messageRepository.GetTotalCountAsync(request.UserId);
    }
}
