using System;
using Shared;

namespace Core.Domain.Entities;

/// <summary>
/// Assignment of a support user to a branch. Many-to-many relationship.
/// </summary>
public class SupportUserBranch : BaseEntity
{
    public Guid SupportUserId { get; private set; }
    public SupportUser SupportUser { get; private set; } = null!;
    public Guid BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;

    private SupportUserBranch() { } // EF Core

    public SupportUserBranch(Guid supportUserId, Guid branchId)
    {
        if (supportUserId == Guid.Empty)
            throw new ArgumentException("Support user ID is required.", nameof(supportUserId));
        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch ID is required.", nameof(branchId));

        SupportUserId = supportUserId;
        BranchId = branchId;
    }
}
