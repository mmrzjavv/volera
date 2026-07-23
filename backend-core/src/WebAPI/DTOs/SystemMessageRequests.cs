using System;

namespace WebAPI.DTOs;

public record CreateSystemMessageRequest(string Title, string Content, DateTime? ExpiresAt);
public record UpdateSystemMessageRequest(string Title, string Content, DateTime? ExpiresAt);
public record SystemMessageResponse(Guid Id, string Title, string Content, DateTime CreatedAt, DateTime? ExpiresAt, bool IsActive, bool IsRead);
