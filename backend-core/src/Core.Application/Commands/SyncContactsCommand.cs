using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Commands;

public class SyncContactsCommand : IRequest<IEnumerable<ContactDto>>
{
    public Guid UserId { get; set; }
    public List<string> PhoneNumbers { get; set; } = new();
}
