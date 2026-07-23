using MediatR;
using System;

namespace Core.Application.Commands;

public class DeleteMessageCommand : IRequest<bool>
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; } // For authorization
}
