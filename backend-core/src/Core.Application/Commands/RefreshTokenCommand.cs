using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Commands;

public class RefreshTokenCommand : IRequest<AuthResponseDto>
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string? AppVersion { get; set; }
}
