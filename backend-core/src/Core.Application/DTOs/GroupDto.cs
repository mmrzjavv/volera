namespace Core.Application.DTOs;

public class GroupDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid AdminId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Kind { get; set; } = "Group";
    public bool IsChannel { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsPublic { get; set; }
    public string? PublicUsername { get; set; }
    public bool CanPost { get; set; }
}
