using MediatR;
using Core.Application.Queries;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetCompanyClientMessagesQueryHandler : IRequestHandler<GetCompanyClientMessagesQuery, IEnumerable<Message>?>
{
    private readonly IMessageRepository _messageRepository;

    public GetCompanyClientMessagesQueryHandler(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<IEnumerable<Message>?> Handle(GetCompanyClientMessagesQuery request, CancellationToken cancellationToken)
    {
        if (!request.ClientUserId.HasValue || !request.BranchId.HasValue)
            return null;

        return await _messageRepository.GetByBranchIdAndClientUserIdAsync(
            request.BranchId.Value,
            request.ClientUserId.Value,
            request.Limit,
            request.Before,
            cancellationToken);
    }
}
