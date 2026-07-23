using MediatR;

namespace Core.Application.Commands;

public class AddOrUpdateReactionCommand : IRequest
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public string Emoji { get; set; } = string.Empty;
}

