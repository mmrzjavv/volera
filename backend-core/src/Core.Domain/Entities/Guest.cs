using System;
using Shared;

namespace Core.Domain.Entities;

/// <summary>
/// Guest aggregate root for guest chat. The linked User exists only for Message.SenderId and is not used for login.
/// </summary>
public class Guest : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Email { get; private set; }
    public string? Mobile { get; private set; }
    public string SessionTokenHash { get; private set; } = string.Empty;
    public DateTime TokenExpiresAt { get; private set; }

    private Guest() { } // EF Core

    /// <summary>
    /// Creates a Guest. At least one of email or mobile must be non-null and non-empty (enforced by application validation).
    /// </summary>
    public Guest(
        Guid userId,
        string sessionTokenHash,
        DateTime tokenExpiresAt,
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        string? mobile = null)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(mobile))
            throw new ArgumentException("At least one of Email or Mobile must be provided.", nameof(email));

        UserId = userId;
        SessionTokenHash = sessionTokenHash;
        TokenExpiresAt = tokenExpiresAt;
        FirstName = firstName;
        LastName = lastName;
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Mobile = string.IsNullOrWhiteSpace(mobile) ? null : mobile.Trim();
    }
}
