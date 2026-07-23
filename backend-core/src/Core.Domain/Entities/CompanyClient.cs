using System;
using Shared;

namespace Core.Domain.Entities;

/// <summary>
/// Company widget visitor session. Similar to Guest but scoped to company/branch.
/// Links to User entity (CompanyClient role) for Message.SenderId.
/// </summary>
public class CompanyClient : BaseEntity
{
    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public Guid BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;
    public Guid CompanyWidgetId { get; private set; }
    public CompanyWidget CompanyWidget { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Email { get; private set; }
    public string? Mobile { get; private set; }
    public string SessionTokenHash { get; private set; } = string.Empty;
    public DateTime TokenExpiresAt { get; private set; }

    private CompanyClient() { } // EF Core

    public CompanyClient(
        Guid companyId,
        Guid branchId,
        Guid companyWidgetId,
        Guid userId,
        string sessionTokenHash,
        DateTime tokenExpiresAt,
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        string? mobile = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID is required.", nameof(companyId));
        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch ID is required.", nameof(branchId));
        if (companyWidgetId == Guid.Empty)
            throw new ArgumentException("Company widget ID is required.", nameof(companyWidgetId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(sessionTokenHash))
            throw new ArgumentException("Session token hash is required.", nameof(sessionTokenHash));

        CompanyId = companyId;
        BranchId = branchId;
        CompanyWidgetId = companyWidgetId;
        UserId = userId;
        SessionTokenHash = sessionTokenHash;
        TokenExpiresAt = tokenExpiresAt;
        FirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim();
        LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Mobile = string.IsNullOrWhiteSpace(mobile) ? null : mobile.Trim();
    }
}
