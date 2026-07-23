using System.Collections.Concurrent;
using Core.Application.Interfaces;

namespace WebAPI.Services;

/// <summary>
/// In-memory fallback when Redis is not configured. Jobs are lost on restart.
/// </summary>
public class InMemoryAiJobQueue : IAiJobQueue
{
    private static readonly ConcurrentQueue<string> IngestQueue = new();
    private static readonly ConcurrentQueue<string> ChatQueue = new();

    public Task EnqueueIngestAsync(string tenantId, string content, Guid companyId, Guid branchId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            TenantId = tenantId,
            Content = content,
            CompanyId = companyId,
            BranchId = branchId,
            JobId = jobId
        });
        IngestQueue.Enqueue(payload);
        return Task.CompletedTask;
    }

    public Task EnqueueChatAsync(string tenantId, string message, string? sessionId, string connectionId, string correlationId, CancellationToken cancellationToken = default)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            TenantId = tenantId,
            Message = message,
            SessionId = sessionId,
            ConnectionId = connectionId,
            CorrelationId = correlationId
        });
        ChatQueue.Enqueue(payload);
        return Task.CompletedTask;
    }

    public Task<(string? Payload, string? JobType)> DequeueAsync(CancellationToken cancellationToken = default)
    {
        if (IngestQueue.TryDequeue(out var ingest))
            return Task.FromResult<(string?, string?)>((ingest, "Ingest"));
        if (ChatQueue.TryDequeue(out var chat))
            return Task.FromResult<(string?, string?)>((chat, "Chat"));
        return Task.FromResult<(string?, string?)>((null, null));
    }

    public Task<(string? Payload, string? JobType)> DequeueIngestAsync(CancellationToken cancellationToken = default)
    {
        if (IngestQueue.TryDequeue(out var payload))
            return Task.FromResult<(string?, string?)>((payload, "Ingest"));
        return Task.FromResult<(string?, string?)>((null, null));
    }

    public Task<(string? Payload, string? JobType)> DequeueChatAsync(CancellationToken cancellationToken = default)
    {
        if (ChatQueue.TryDequeue(out var payload))
            return Task.FromResult<(string?, string?)>((payload, "Chat"));
        return Task.FromResult<(string?, string?)>((null, null));
    }
}
