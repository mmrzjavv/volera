using System;
using Shared;

namespace Core.Domain.Entities;

/// <summary>
/// Widget configuration for a company branch. Each branch gets a unique widget ID.
/// </summary>
public class CompanyWidget : BaseEntity
{
    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public Guid BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;
    public string WidgetId { get; private set; } = string.Empty;
    public string? WidgetTokenHash { get; private set; }
    public bool IsActive { get; private set; } = true;

    private CompanyWidget() { } // EF Core

    public CompanyWidget(Guid companyId, Guid branchId, string widgetId, string? widgetTokenHash = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID is required.", nameof(companyId));
        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch ID is required.", nameof(branchId));
        if (string.IsNullOrWhiteSpace(widgetId))
            throw new ArgumentException("Widget ID is required.", nameof(widgetId));

        CompanyId = companyId;
        BranchId = branchId;
        WidgetId = widgetId.Trim();
        WidgetTokenHash = widgetTokenHash;
    }

    public void SetWidgetToken(string? tokenHash)
    {
        WidgetTokenHash = tokenHash;
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
