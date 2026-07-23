namespace Core.Application.DTOs;

public class UpdateProfileDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePicture { get; set; }
}