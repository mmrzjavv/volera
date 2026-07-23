namespace Core.Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Username { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsOnline { get; set; }
    public string Role { get; set; } = "User";
}