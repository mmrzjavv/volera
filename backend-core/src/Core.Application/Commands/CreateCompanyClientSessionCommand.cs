using MediatR;

namespace Core.Application.Commands;

public class CreateCompanyClientSessionCommand : IRequest<CreateCompanyClientSessionResult?>
{
    public string WidgetId { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
}

public class CreateCompanyClientSessionResult
{
    public string ClientToken { get; init; } = string.Empty;
    public Guid ClientId { get; init; }
    public DateTime ExpiresAt { get; init; }
}
