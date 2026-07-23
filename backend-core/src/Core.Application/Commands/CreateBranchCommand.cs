using MediatR;

namespace Core.Application.Commands;

public class CreateBranchCommand : IRequest<Guid>
{
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}
