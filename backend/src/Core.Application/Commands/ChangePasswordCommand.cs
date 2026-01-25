using MediatR;

namespace Core.Application.Commands;

public class ChangePasswordCommand : IRequest
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}