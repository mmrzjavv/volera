namespace Core.Application.DTOs;

public class InitiateCallDto
{
    public Guid ReceiverId { get; set; }
    public bool IsVideo { get; set; }
}