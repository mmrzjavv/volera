using MediatR;

namespace Core.Application.Commands;

public class RegisterUserCommand : IRequest<Guid>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
}