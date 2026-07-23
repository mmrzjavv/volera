using MediatR;

namespace Core.Application.Queries;

public class GetCompanyBranchesQuery : IRequest<IEnumerable<BranchDto>>
{
    public Guid CompanyId { get; set; }
}

public class BranchDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}
