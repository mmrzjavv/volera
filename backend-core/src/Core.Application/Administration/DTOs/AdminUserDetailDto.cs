namespace Core.Application.Administration.DTOs;

public class AdminUserDetailDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePicture { get; set; }
    public string Role { get; set; } = "";
    public bool IsDisabled { get; set; }
    public DateTime? SuspendedUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    public int ChatCount { get; set; }
    public int SavedMessagesCount { get; set; }
    public long StorageUsedBytes { get; set; }
    public IEnumerable<AdminLimitOverrideDto> LimitOverrides { get; set; } = new List<AdminLimitOverrideDto>();
}

public class AdminLimitOverrideDto
{
    public string LimitKey { get; set; } = "";
    public decimal Value { get; set; }
}
