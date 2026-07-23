using Core.Application.DTOs;
using MediatR;

namespace Core.Application.Queries;

public class GetUnreadCountsQuery : IRequest<IEnumerable<UnreadCountDto>>
{
    public Guid UserId { get; set; }
}
