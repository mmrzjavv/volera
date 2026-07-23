namespace Core.Application.Interfaces;

/// <summary>
/// Queue for AI widget jobs: ingest (text content) and chat. Payload is text only; no file URL.
/// </summary>
public interface IAiJobQueue
{
    Task EnqueueIngestAsync(string tenantId, string content, Guid companyId, Guid branchId, Guid jobId, CancellationToken cancellationToken = default);
    Task EnqueueChatAsync(string tenantId, string message, string? sessionId, string connectionId, string correlationId, CancellationToken cancellationToken = default);
    Task<(string? Payload, string? JobType)> DequeueAsync(CancellationToken cancellationToken = default);
    Task<(string? Payload, string? JobType)> DequeueIngestAsync(CancellationToken cancellationToken = default);
    Task<(string? Payload, string? JobType)> DequeueChatAsync(CancellationToken cancellationToken = default);
}
