using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetMessagesQuery : IRequest<List<MessageDto>>
{
    public Guid UserId { get; set; } // The other user in the conversation
    public Guid CurrentUserId { get; set; } // The user requesting the messages
    public DateTime? Before { get; set; } // Cursor for pagination
    public int Limit { get; set; } = 20; // Number of messages to fetch
}
