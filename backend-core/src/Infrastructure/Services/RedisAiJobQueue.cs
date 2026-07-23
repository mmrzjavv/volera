using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using StackExchange.Redis;

namespace Infrastructure.Services;

public class RedisAiJobQueue : IAiJobQueue
{
    private const string IngestQueueKey = "ai:queue:ingest";
    private const string ChatQueueKey = "ai:queue:chat";
    private readonly IDatabase _db;

    public RedisAiJobQueue(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task EnqueueIngestAsync(string tenantId, string content, Guid companyId, Guid branchId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var payload = new IngestJobPayload
        {
            TenantId = tenantId,
            Content = content,
            CompanyId = companyId,
            BranchId = branchId,
            JobId = jobId
        };
        var json = JsonSerializer.Serialize(payload);
        await _db.ListLeftPushAsync(IngestQueueKey, json);
    }

    public async Task EnqueueChatAsync(string tenantId, string message, string? sessionId, string connectionId, string correlationId, CancellationToken cancellationToken = default)
    {
        var payload = new ChatJobPayload
        {
            TenantId = tenantId,
            Message = message,
            SessionId = sessionId,
            ConnectionId = connectionId,
            CorrelationId = correlationId
        };
        var json = JsonSerializer.Serialize(payload);
        await _db.ListLeftPushAsync(ChatQueueKey, json);
    }

    public async Task<(string? Payload, string? JobType)> DequeueAsync(CancellationToken cancellationToken = default)
    {
        // Try ingest first, then chat
        var ingest = await _db.ListRightPopAsync(IngestQueueKey);
        if (ingest.HasValue && !ingest.IsNullOrEmpty)
            return (ingest!, "Ingest");

        var chat = await _db.ListRightPopAsync(ChatQueueKey);
        if (chat.HasValue && !chat.IsNullOrEmpty)
            return (chat!, "Chat");

        return (null, null);
    }

    public async Task<(string? Payload, string? JobType)> DequeueIngestAsync(CancellationToken cancellationToken = default)
    {
        var value = await _db.ListRightPopAsync(IngestQueueKey);
        if (value.HasValue && !value.IsNullOrEmpty)
            return (value!, "Ingest");
        return (null, null);
    }

    public async Task<(string? Payload, string? JobType)> DequeueChatAsync(CancellationToken cancellationToken = default)
    {
        var value = await _db.ListRightPopAsync(ChatQueueKey);
        if (value.HasValue && !value.IsNullOrEmpty)
            return (value!, "Chat");
        return (null, null);
    }

    public static IngestJobPayload? DeserializeIngest(string json)
    {
        return JsonSerializer.Deserialize<IngestJobPayload>(json);
    }

    public static ChatJobPayload? DeserializeChat(string json)
    {
        return JsonSerializer.Deserialize<ChatJobPayload>(json);
    }

    public class IngestJobPayload
    {
        public string TenantId { get; set; } = "";
        public string Content { get; set; } = "";
        public Guid CompanyId { get; set; }
        public Guid BranchId { get; set; }
        public Guid JobId { get; set; }
    }

    public class ChatJobPayload
    {
        public string TenantId { get; set; } = "";
        public string Message { get; set; } = "";
        public string? SessionId { get; set; }
        public string ConnectionId { get; set; } = "";
        public string CorrelationId { get; set; } = "";
    }
}
