using MediatR;

namespace Core.Application.Commands;

public class UpdateProfileCommand : IRequest
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? ProfilePicture { get; set; }
}