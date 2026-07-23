using Core.Application.Interfaces;
using Hangfire;

namespace WebAPI.Services;

public class HangfireAiJobEnqueuer : IAiJobEnqueuer
{
    public void EnqueueIngest(Guid jobId, string tenantId, string content, Guid companyId, Guid branchId)
    {
        BackgroundJob.Enqueue<AiIngestJob>(x => x.Process(jobId, tenantId, content, companyId, branchId));
    }

    public void EnqueueChat(string tenantId, string message, string? sessionId, string connectionId, string correlationId)
    {
        BackgroundJob.Enqueue<AiChatJob>(x => x.Process(tenantId, message, sessionId, connectionId, correlationId));
    }
}
