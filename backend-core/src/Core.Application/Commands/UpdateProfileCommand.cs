using MediatR;

namespace Core.Application.Commands;

public class UpdateProfileCommand : IRequest
{
    public Guid UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePicture { get; set; }
}