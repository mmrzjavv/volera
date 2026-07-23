using MediatR;

namespace Core.Application.Commands;

public class UpdateSupportUserCommand : IRequest
{
    public Guid SupportUserId { get; set; }
    public Guid CompanyId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}
