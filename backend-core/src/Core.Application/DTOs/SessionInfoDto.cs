namespace Core.Application.DTOs;

public class SessionInfoDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OS { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime LoginAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public string AppVersion { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
}
