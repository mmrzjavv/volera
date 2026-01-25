using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetCallsByUserIdQuery : IRequest<IEnumerable<CallDto>>
{
    public Guid UserId { get; set; }
}