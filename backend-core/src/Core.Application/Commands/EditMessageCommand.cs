using MediatR;
using System;

namespace Core.Application.Commands;

public class EditMessageCommand : IRequest<bool>
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; } // For authorization
    public required string NewContent { get; set; }
}
