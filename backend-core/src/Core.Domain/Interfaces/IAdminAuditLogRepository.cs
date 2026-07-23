using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IAdminAuditLogRepository : IRepository<AdminAuditLog>
{
    Task<(IEnumerable<AdminAuditLog> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? adminUserId = null,
        string? action = null,
        string? resourceType = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}
