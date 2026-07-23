using MediatR;

namespace Core.Application.Commands;

public class RegisterCompanyCommand : IRequest<RegisterCompanyResult>
{
    public required string Name { get; set; }
    public required string MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class RegisterCompanyResult
{
    public Guid CompanyId { get; init; }
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}
