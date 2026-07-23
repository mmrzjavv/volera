using MediatR;

namespace Core.Application.Queries;

public class GetCompanyByIdQuery : IRequest<CompanyDto?>
{
    public Guid CompanyId { get; set; }
}

public class CompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
}
