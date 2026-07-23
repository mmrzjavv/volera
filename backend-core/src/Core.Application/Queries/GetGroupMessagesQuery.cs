using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetGroupMessagesQuery : IRequest<List<MessageDto>>
{
    public Guid GroupId { get; set; }
    public Guid CurrentUserId { get; set; }
    public DateTime? Before { get; set; } // Cursor for pagination
    public int Limit { get; set; } = 20; // Number of messages to fetch
}
