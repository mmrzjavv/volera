using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetRecentChatsQuery : IRequest<IEnumerable<RecentChatDto>>
{
    public Guid UserId { get; set; }
}