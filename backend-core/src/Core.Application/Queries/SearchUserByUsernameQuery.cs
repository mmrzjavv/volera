using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Queries;

public class SearchUserByUsernameQuery : IRequest<UserDto?>
{
    public string Username { get; set; } = string.Empty;
}
