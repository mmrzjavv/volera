namespace Core.Application.DTOs;

public record SystemMessageDto(
    Guid Id,
    string Title,
    string Content,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    bool IsActive,
    bool IsRead);

