using Core.Application.DTOs;
using MediatR;

namespace Core.Application.Queries;

public class GetGroupDetailsQuery : IRequest<GroupDetailsDto>
{
    public Guid GroupId { get; set; }
    public Guid CurrentUserId { get; set; }
}

