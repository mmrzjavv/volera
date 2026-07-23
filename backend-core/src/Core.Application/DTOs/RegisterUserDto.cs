namespace Core.Application.DTOs;

public class RegisterUserDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Username { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Password { get; set; }
}