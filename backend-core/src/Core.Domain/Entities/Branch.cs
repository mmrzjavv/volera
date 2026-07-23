using System;
using Shared;

namespace Core.Domain.Entities;

/// <summary>
/// Branch of a company. Each branch has its own inbox/conversation.
/// </summary>
public class Branch : BaseEntity
{
    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Branch() { } // EF Core

    public Branch(Guid companyId, string name, string? address = null, string? phoneNumber = null, string? email = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID is required.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Branch name is required.", nameof(name));

        CompanyId = companyId;
        Name = name.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    public void Update(string name, string? address, string? phoneNumber, string? email)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
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
