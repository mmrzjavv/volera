namespace Core.Application.Administration.DTOs;

public class SystemLimitDto
{
    public string Key { get; set; } = "";
    public decimal Value { get; set; }
    public string? Description { get; set; }
}
