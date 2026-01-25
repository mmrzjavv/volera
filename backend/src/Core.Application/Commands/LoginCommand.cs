using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Commands;

public class LoginCommand : IRequest<AuthResponseDto>
{
    public string Username { get; set; }
    public string Password { get; set; }
}