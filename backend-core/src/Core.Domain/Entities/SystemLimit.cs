using System;
using Shared;

namespace Core.Domain.Entities;

public class SystemLimit : BaseEntity
{
    public string Key { get; private set; }
    public decimal Value { get; private set; }
    public string? Description { get; private set; }

    private SystemLimit() { } // EF Core

    public SystemLimit(string key, decimal value, string? description = null)
    {
        Key = key;
        Value = value;
        Description = description;
    }

    public void SetValue(decimal value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}

public static class LimitKeys
{
    public const string MaxPinnedMessages = "MaxPinnedMessages";
    public const string MaxSavedMessagesSizeBytes = "MaxSavedMessagesSizeBytes";
    public const string MaxSavedMessagesCount = "MaxSavedMessagesCount";
    public const string MaxSessionsPerUser = "MaxSessionsPerUser";
    public const string MaxGuestMessagesPerMinute = "MaxGuestMessagesPerMinute";
    public const string MaxGuestSessionsPerIpPerHour = "MaxGuestSessionsPerIpPerHour";
    public const string MaxMessageLength = "MaxMessageLength";
}
