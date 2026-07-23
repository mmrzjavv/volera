namespace Core.Application.Administration.DTOs;

public class SystemStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalMessages { get; set; }
    public int TotalGroups { get; set; }
    public long StorageUsedBytes { get; set; }
    public int UsersOverLimit { get; set; }
}
