using System;
using Shared;

namespace Core.Domain.Entities;

/// <summary>
/// Company entity. Separate from User. Registration via mobile number (OTP TODO).
/// </summary>
public class Company : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string MobileNumber { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? LogoUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? RegistrationTokenHash { get; private set; }
    public DateTime? TokenExpiresAt { get; private set; }

    private Company() { } // EF Core

    public Company(
        string name,
        string mobileNumber,
        string? email = null,
        string? address = null,
        string? logoUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(mobileNumber))
            throw new ArgumentException("Mobile number is required.", nameof(mobileNumber));

        Name = name.Trim();
        MobileNumber = mobileNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
    }

    public void UpdateProfile(string name, string? email, string? address, string? logoUrl)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRegistrationToken(string? tokenHash, DateTime? expiresAt)
    {
        RegistrationTokenHash = tokenHash;
        TokenExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
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
