using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Commands;

public class RefreshSupportUserTokenCommand : IRequest<SupportUserAuthResultDto?>
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}
