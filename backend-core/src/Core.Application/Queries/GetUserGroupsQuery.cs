using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetUserGroupsQuery : IRequest<List<GroupDto>>
{
    public Guid UserId { get; set; }
}
