using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetUsersQuery : IRequest<List<UserDto>>
{
    public Guid? ExcludeUserId { get; set; }
}