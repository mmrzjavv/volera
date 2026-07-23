using MediatR;

namespace Core.Application.Commands;

public class UpdateBranchCommand : IRequest
{
    public Guid BranchId { get; set; }
    public Guid CompanyId { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}
