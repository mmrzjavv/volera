using System;
using Shared;

namespace Core.Domain.Entities;

/// <summary>
/// Support user belonging to a company. Separate from User entity. Username/password authentication.
/// </summary>
public class SupportUser : BaseEntity
{
    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; } = true;
    public SupportUserRole Role { get; private set; } = SupportUserRole.SupportAgent;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }

    private SupportUser() { } // EF Core

    public SupportUser(
        Guid companyId,
        string username,
        string passwordHash,
        string firstName,
        string lastName,
        SupportUserRole role = SupportUserRole.SupportAgent,
        string? email = null,
        string? phoneNumber = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID is required.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        CompanyId = companyId;
        Username = username.Trim();
        PasswordHash = passwordHash;
        FirstName = firstName?.Trim() ?? string.Empty;
        LastName = lastName?.Trim() ?? string.Empty;
        Role = role;
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
    }

    public void UpdateProfile(string firstName, string lastName, string? email, string? phoneNumber)
    {
        FirstName = firstName?.Trim() ?? FirstName;
        LastName = lastName?.Trim() ?? LastName;
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRole(SupportUserRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRefreshToken(string? refreshToken, DateTime? expiryTime)
    {
        RefreshToken = refreshToken;
        RefreshTokenExpiryTime = expiryTime;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
