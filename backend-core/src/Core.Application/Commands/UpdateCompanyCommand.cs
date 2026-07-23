using MediatR;

namespace Core.Application.Commands;

public class UpdateCompanyCommand : IRequest
{
    public Guid CompanyId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
}
