using Core.Application.Interfaces;
using Core.Application.Logging;
using Core.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;

namespace WebAPI.Services;

public class AiIngestJob
{
    private readonly IAiServiceClient _aiService;
    private readonly IAiContentBlockRepository _contentBlockRepository;
    private readonly ICompanyAiWidgetRepository _aiWidgetRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<AiWidgetHub> _hubContext;
    private readonly ILogger<AiIngestJob> _logger;

    public AiIngestJob(
        IAiServiceClient aiService,
        IAiContentBlockRepository contentBlockRepository,
        ICompanyAiWidgetRepository aiWidgetRepository,
        IUnitOfWork unitOfWork,
        IHubContext<AiWidgetHub> hubContext,
        ILogger<AiIngestJob> logger)
    {
        _aiService = aiService;
        _contentBlockRepository = contentBlockRepository;
        _aiWidgetRepository = aiWidgetRepository;
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Process(Guid jobId, string tenantId, string content, Guid companyId, Guid branchId)
    {
        var block = await _contentBlockRepository.GetByJobIdAsync(jobId);
        if (block != null)
        {
            block.SetProcessing();
            _contentBlockRepository.Update(block);
            await _unitOfWork.SaveChangesAsync();
        }

        try
        {
            if (block == null)
                return;
            var contentToEmbed = block.Content;
            if (string.IsNullOrWhiteSpace(contentToEmbed))
                contentToEmbed = content;
            var embedding = await _aiService.GetEmbeddingAsync(contentToEmbed);
            var embeddingJson = System.Text.Json.JsonSerializer.Serialize(embedding);
            block.SetCompleted(embeddingJson);
            _contentBlockRepository.Update(block);
            await _unitOfWork.SaveChangesAsync();
            // Activate widget when first content is successfully indexed
            var widget = await _aiWidgetRepository.GetByIdAsync(block.CompanyAiWidgetId);
            if (widget != null && !widget.IsActive)
            {
                widget.Activate();
                _aiWidgetRepository.Update(widget);
                await _unitOfWork.SaveChangesAsync();
            }
            await _hubContext.Clients.Group(AiWidgetHub.CompanyGroupPrefix + companyId)
                .SendAsync("ContentIndexed", jobId, branchId, "Completed", (string?)null);
        }
        catch (Exception ex)
        {
            AppLog.Warning(_logger, AppLogEvents.AiIngestFailed, ex,
                "JobId: {JobId} | TenantId: {TenantId} | CompanyId: {CompanyId} | Error: {ErrorType} | Result: Failure",
                jobId, tenantId, companyId, ex.GetType().Name);
            if (block != null)
            {
                block.SetFailed(ex.Message);
                _contentBlockRepository.Update(block);
                await _unitOfWork.SaveChangesAsync();
            }
            await _hubContext.Clients.Group(AiWidgetHub.CompanyGroupPrefix + companyId)
                .SendAsync("ContentIndexed", jobId, branchId, "Failed", ex.Message);
        }
    }
}
