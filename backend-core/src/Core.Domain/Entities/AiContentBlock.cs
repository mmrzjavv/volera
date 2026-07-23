using System;
using Shared;

namespace Core.Domain.Entities;

/// <summary>
/// A block of company text submitted for RAG indexing. Text-only; no file. Worker updates Status when ingest completes.
/// </summary>
public class AiContentBlock : BaseEntity
{
    public Guid BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;
    public Guid CompanyAiWidgetId { get; private set; }
    public CompanyAiWidget CompanyAiWidget { get; private set; } = null!;
    /// <summary>First N characters of the submitted text for display in lists.</summary>
    public string ContentSnippet { get; private set; } = string.Empty;
    /// <summary>Full content for RAG. Stored in DB; Python reads from Postgres for vector search.</summary>
    public string Content { get; private set; } = string.Empty;
    /// <summary>Embedding vector as JSON array of numbers. Set by ingest job after calling Python /embed.</summary>
    public string? EmbeddingJson { get; private set; }
    public AiContentBlockStatus Status { get; private set; } = AiContentBlockStatus.Pending;
    public Guid? JobId { get; private set; }
    public string? ErrorMessage { get; private set; }

    private AiContentBlock() { } // EF Core

    public AiContentBlock(Guid branchId, Guid companyAiWidgetId, string contentSnippet, string fullContent, Guid jobId)
    {
        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch ID is required.", nameof(branchId));
        if (companyAiWidgetId == Guid.Empty)
            throw new ArgumentException("Company AI Widget ID is required.", nameof(companyAiWidgetId));
        if (string.IsNullOrEmpty(contentSnippet))
            contentSnippet = "(empty)";
        if (contentSnippet.Length > 500)
            contentSnippet = contentSnippet.Substring(0, 500);
        BranchId = branchId;
        CompanyAiWidgetId = companyAiWidgetId;
        ContentSnippet = contentSnippet;
        Content = fullContent ?? "";
        JobId = jobId;
    }

    public void SetProcessing()
    {
        Status = AiContentBlockStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCompleted(string? embeddingJson = null)
    {
        Status = AiContentBlockStatus.Completed;
        ErrorMessage = null;
        if (embeddingJson != null)
            EmbeddingJson = embeddingJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFailed(string? errorMessage)
    {
        Status = AiContentBlockStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum AiContentBlockStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
