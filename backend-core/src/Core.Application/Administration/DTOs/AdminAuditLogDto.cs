namespace Core.Application.Administration.DTOs;

public class AdminAuditLogDto
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string? AdminUsername { get; set; }
    public string Action { get; set; } = "";
    public string ResourceType { get; set; } = "";
    public Guid? ResourceId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
