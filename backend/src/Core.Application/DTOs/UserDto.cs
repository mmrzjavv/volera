namespace Core.Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsOnline { get; set; }
}