using System;

namespace Core.Application.DTOs;

public class SavedMessageDto
{
    public Guid Id { get; set; } // SavedMessage ID
    public Guid MessageId { get; set; }
    public required MessageDto Message { get; set; }
    public DateTime SavedAt { get; set; }
}
