using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetContactsQuery : IRequest<IEnumerable<ContactDto>>
{
    public Guid UserId { get; set; }
    public string? Status { get; set; }
}
