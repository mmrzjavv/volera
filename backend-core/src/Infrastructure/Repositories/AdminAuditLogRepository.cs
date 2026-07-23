using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AdminAuditLogRepository : Repository<AdminAuditLog>, IAdminAuditLogRepository
{
    public AdminAuditLogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<(IEnumerable<AdminAuditLog> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? adminUserId = null,
        string? action = null,
        string? resourceType = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AdminAuditLogs.AsNoTracking().AsQueryable();

        if (adminUserId.HasValue)
            query = query.Where(a => a.AdminUserId == adminUserId.Value);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(resourceType))
            query = query.Where(a => a.ResourceType == resourceType);
        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
