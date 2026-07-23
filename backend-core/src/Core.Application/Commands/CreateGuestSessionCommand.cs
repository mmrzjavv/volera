using MediatR;

namespace Core.Application.Commands;

public class CreateGuestSessionCommand : IRequest<CreateGuestSessionResult>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
}

public class CreateGuestSessionResult
{
    public string GuestToken { get; init; } = string.Empty;
    public Guid GuestId { get; init; }
    public DateTime ExpiresAt { get; init; }
}
