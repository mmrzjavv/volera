using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class GetUserByIdQuery : IRequest<UserDto?>
{
    public Guid UserId { get; set; }
}