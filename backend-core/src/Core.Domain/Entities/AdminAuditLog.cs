using System;
using Shared;

namespace Core.Domain.Entities;

public class AdminAuditLog : BaseEntity
{
    public Guid AdminUserId { get; private set; }
    public string Action { get; private set; }
    public string ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public string? Details { get; private set; }

    private AdminAuditLog() { } // EF Core

    public AdminAuditLog(Guid adminUserId, string action, string resourceType, Guid? resourceId = null, string? details = null)
    {
        AdminUserId = adminUserId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Details = details;
    }
}

public static class AdminAuditActions
{
    public const string DisableUser = "DisableUser";
    public const string SuspendUser = "SuspendUser";
    public const string ReactivateUser = "ReactivateUser";
    public const string SetRole = "SetRole";
    public const string AdminUpdateUser = "AdminUpdateUser";
    public const string AdminEditMessage = "AdminEditMessage";
    public const string AdminDeleteMessage = "AdminDeleteMessage";
    public const string SetSystemLimit = "SetSystemLimit";
    public const string SetUserLimitOverride = "SetUserLimitOverride";
    public const string RemoveUserLimitOverride = "RemoveUserLimitOverride";
    public const string AdminPurgeConversation = "AdminPurgeConversation";
}

public static class AdminResourceTypes
{
    public const string User = "User";
    public const string Message = "Message";
    public const string Chat = "Chat";
    public const string Limit = "Limit";
}
