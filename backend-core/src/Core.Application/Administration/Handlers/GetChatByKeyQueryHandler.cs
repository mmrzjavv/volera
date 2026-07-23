using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetChatByKeyQueryHandler : IRequestHandler<GetChatByKeyQuery, AdminChatDto?>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetChatByKeyQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<AdminChatDto?> Handle(GetChatByKeyQuery request, CancellationToken cancellationToken)
    {
        return await _adminReadRepository.GetChatByKeyAsync(request.ConversationKey, cancellationToken);
    }
}
