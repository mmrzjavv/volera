using MediatR;

namespace Core.Application.Commands;

public class ChangePasswordCommand : IRequest
{
    public Guid UserId { get; set; }
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}