using System;
using Shared;

namespace Core.Domain.Entities;

public class UserLimitOverride : BaseEntity
{
    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public string LimitKey { get; private set; }
    public decimal Value { get; private set; }

    private UserLimitOverride() { } // EF Core

    public UserLimitOverride(Guid userId, string limitKey, decimal value)
    {
        UserId = userId;
        LimitKey = limitKey;
        Value = value;
    }

    public void SetValue(decimal value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}
