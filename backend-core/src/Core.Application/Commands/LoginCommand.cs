using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Commands;

public class LoginCommand : IRequest<AuthResponseDto>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public string? OS { get; set; }
    public string? Location { get; set; }
    public string? AppVersion { get; set; }
}