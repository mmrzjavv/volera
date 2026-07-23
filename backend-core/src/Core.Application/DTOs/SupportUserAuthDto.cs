namespace Core.Application.DTOs;

public class SupportUserAuthResultDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public SupportUserDto SupportUser { get; set; } = null!;
}

public class SupportUserDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
}
