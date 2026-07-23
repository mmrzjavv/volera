using System;
using Shared;

namespace Core.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Username { get; private set; }
    public string PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Bio { get; private set; }
    public string PasswordHash { get; private set; }
    public string? ProfilePicture { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }
    public UserRole Role { get; private set; } = UserRole.User;
    public bool IsDisabled { get; private set; }
    public DateTime? SuspendedUntil { get; private set; }

    private User() { } // EF Core

    public User(string firstName, string lastName, string username, string phoneNumber, string passwordHash, UserRole role = UserRole.User)
    {
        FirstName = firstName;
        LastName = lastName;
        Username = username;
        PhoneNumber = phoneNumber;
        PasswordHash = passwordHash;
        Role = role;
    }

    public void Disable(Guid adminUserId)
    {
        if (Id == adminUserId)
            throw new InvalidOperationException("Cannot disable your own account.");
        IsDisabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend(DateTime until, Guid adminUserId)
    {
        if (Id == adminUserId)
            throw new InvalidOperationException("Cannot suspend your own account.");
        if (until <= DateTime.UtcNow)
            throw new ArgumentException("Suspension end time must be in the future.", nameof(until));
        SuspendedUntil = until;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        IsDisabled = false;
        SuspendedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRole(UserRole newRole, int superAdminCount)
    {
        if (Role == UserRole.SuperAdmin && newRole != UserRole.SuperAdmin)
        {
            if (superAdminCount <= 1)
                throw new InvalidOperationException("Cannot remove the last SuperAdmin.");
        }
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsSuspended => SuspendedUntil.HasValue && SuspendedUntil.Value > DateTime.UtcNow;
    public bool CanUseSystem => !IsDisabled && !IsSuspended;

    public void UpdateProfile(string firstName, string lastName, string? profilePicture, string? email, string? bio)
    {
        FirstName = firstName;
        LastName = lastName;
        ProfilePicture = profilePicture;
        Email = email;
        Bio = bio;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRefreshToken(string refreshToken, DateTime expiryTime)
    {
        RefreshToken = refreshToken;
        RefreshTokenExpiryTime = expiryTime;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }
}
