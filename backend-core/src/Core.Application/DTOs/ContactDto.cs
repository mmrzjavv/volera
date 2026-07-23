using Core.Domain.Entities;

namespace Core.Application.DTOs;

public class ContactDto
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? ContactUserId { get; set; }
    public UserDto? ContactUser { get; set; }
    public string? ContactName { get; set; }
    public required string ContactPhoneNumber { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
