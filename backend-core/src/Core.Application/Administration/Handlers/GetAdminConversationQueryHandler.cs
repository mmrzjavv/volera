using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetAdminConversationQueryHandler : IRequestHandler<GetAdminConversationQuery, AdminConversationResultDto>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetAdminConversationQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<AdminConversationResultDto> Handle(GetAdminConversationQuery request, CancellationToken cancellationToken)
    {
        var (messages, nextCursor, hasMore, title, type) = await _adminReadRepository.GetConversationMessagesAsync(
            request.ConversationKey, request.Limit, request.Before, cancellationToken);
        return new AdminConversationResultDto
        {
            Messages = messages,
            NextCursor = nextCursor,
            HasMore = hasMore,
            ConversationKey = request.ConversationKey,
            ConversationTitle = title,
            Type = type
        };
    }
}
