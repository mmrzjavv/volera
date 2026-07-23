using MediatR;
using Core.Application.DTOs;

namespace Core.Application.Commands;

public class SupportUserLoginCommand : IRequest<SupportUserAuthResultDto?>
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public Guid CompanyId { get; set; }
}
