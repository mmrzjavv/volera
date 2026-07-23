using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Commands;

/// <summary>Support user login with username and password only (no company ID).</summary>
public class SupportUserLoginByUsernameCommand : IRequest<SupportUserAuthResultDto?>
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
