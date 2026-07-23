using MediatR;

namespace Core.Application.Commands;

public class MarkMessagesAsReadCommand : IRequest<bool>
{
    public Guid UserId { get; set; } // The user reading the messages (Receiver)
    public Guid SenderId { get; set; } // The user who sent the messages
}
