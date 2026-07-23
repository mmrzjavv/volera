using System;
using Shared;

namespace Core.Domain.Entities;

/// <summary>
/// AI Widget configuration for a company branch. One per branch; TenantId is used by the Python AI service for RAG.
/// </summary>
public class CompanyAiWidget : BaseEntity
{
    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public Guid BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;
    /// <summary>Unique tenant identifier for the Python AI service (e.g. companyId_branchId).</summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>True only after at least one content block has been indexed (Completed).</summary>
    public bool IsActive { get; private set; }

    private CompanyAiWidget() { } // EF Core

    public CompanyAiWidget(Guid companyId, Guid branchId, string tenantId)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID is required.", nameof(companyId));
        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch ID is required.", nameof(branchId));
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID is required.", nameof(tenantId));

        CompanyId = companyId;
        BranchId = branchId;
        TenantId = tenantId.Trim();
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
