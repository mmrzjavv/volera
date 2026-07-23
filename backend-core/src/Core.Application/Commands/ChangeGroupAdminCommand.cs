using MediatR;

namespace Core.Application.Commands;

public class ChangeGroupAdminCommand : IRequest
{
    public Guid GroupId { get; set; }
    public Guid CurrentAdminId { get; set; }
    public Guid NewAdminId { get; set; }
}

