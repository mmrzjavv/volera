using MediatR;

namespace Core.Application.Commands;

public class JoinGroupByInviteCommand : IRequest
{
    public string InviteCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}

