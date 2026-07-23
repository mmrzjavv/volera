using MediatR;

namespace Core.Application.Commands;

public class AddContactCommand : IRequest<Guid>
{
    public Guid OwnerUserId { get; set; }
    public string ContactIdentifier { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
}
