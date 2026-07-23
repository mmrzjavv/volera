using Core.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;

namespace WebAPI.Services;

public class AiChatJob
{
    private readonly IAiServiceClient _aiService;
    private readonly IHubContext<AiWidgetHub> _hubContext;
    private readonly ILogger<AiChatJob> _logger;

    public AiChatJob(
        IAiServiceClient aiService,
        IHubContext<AiWidgetHub> hubContext,
        ILogger<AiChatJob> logger)
    {
        _aiService = aiService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Process(string tenantId, string message, string? sessionId, string connectionId, string correlationId)
    {
        try
        {
            var answer = await _aiService.ChatAsync(tenantId, message, sessionId);
            await _hubContext.Clients.Client(connectionId)
                .SendAsync("AiReply", correlationId, answer);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chat failed for correlation {CorrelationId}", correlationId);
            await _hubContext.Clients.Client(connectionId)
                .SendAsync("AiReply", correlationId, $"Sorry, an error occurred: {ex.Message}");
        }
    }
}
