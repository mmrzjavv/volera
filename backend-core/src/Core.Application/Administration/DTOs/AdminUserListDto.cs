namespace Core.Application.Administration.DTOs;

public class AdminUserListDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsDisabled { get; set; }
    public DateTime? SuspendedUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MessageCount { get; set; }
    public int ChatCount { get; set; }
    public int SavedMessagesCount { get; set; }
    public long StorageUsedBytes { get; set; }
}
