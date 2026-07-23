using MediatR;
using Core.Domain.Entities;

namespace Core.Application.Commands;

public class CreateSupportUserCommand : IRequest<Guid>
{
    public Guid CompanyId { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public SupportUserRole Role { get; set; } = SupportUserRole.SupportAgent;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}
