namespace Core.Application.Interfaces;

/// <summary>
/// HTTP client for the Python AI service: embed text (for storage in DB) and chat (RAG from Postgres).
/// </summary>
public interface IAiServiceClient
{
    /// <summary>Get embedding vector for text. .NET stores it in AiContentBlock; Python reads blocks from Postgres for RAG.</summary>
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<string> ChatAsync(string tenantId, string message, string? sessionId, CancellationToken cancellationToken = default);
}
