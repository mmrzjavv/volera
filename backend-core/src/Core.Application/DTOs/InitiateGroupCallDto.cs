namespace Core.Application.DTOs;

public class InitiateGroupCallDto
{
    public Guid GroupId { get; set; }
    public bool IsVideo { get; set; }
}

