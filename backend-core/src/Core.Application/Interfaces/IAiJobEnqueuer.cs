namespace Core.Application.Interfaces;

/// <summary>
/// Enqueues AI widget jobs for background processing (e.g. Hangfire). Used instead of polling.
/// </summary>
public interface IAiJobEnqueuer
{
    void EnqueueIngest(Guid jobId, string tenantId, string content, Guid companyId, Guid branchId);
    void EnqueueChat(string tenantId, string message, string? sessionId, string connectionId, string correlationId);
}
