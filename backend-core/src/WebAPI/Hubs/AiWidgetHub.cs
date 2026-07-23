using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Core.Application.Interfaces;
using Core.Domain.Interfaces;

namespace WebAPI.Hubs;

[AllowAnonymous]
public class AiWidgetHub : Hub
{
    public const string CompanyGroupPrefix = "company_ai_";
    private readonly ICompanyTokenService _companyTokenService;
    private readonly IAiWidgetSessionService _sessionService;
    private readonly ICompanyAiWidgetRepository _aiWidgetRepository;
    private readonly IAiJobEnqueuer _jobEnqueuer;

    public AiWidgetHub(
        ICompanyTokenService companyTokenService,
        IAiWidgetSessionService sessionService,
        ICompanyAiWidgetRepository aiWidgetRepository,
        IAiJobEnqueuer jobEnqueuer)
    {
        _companyTokenService = companyTokenService;
        _sessionService = sessionService;
        _aiWidgetRepository = aiWidgetRepository;
        _jobEnqueuer = jobEnqueuer;
    }

    public override async Task OnConnectedAsync()
    {
        var token = Context.GetHttpContext()?.Request.Query["access_token"].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
        {
            throw new HubException("access_token is required in query string.");
        }

        // Try company token first (admin panel)
        var company = await _companyTokenService.ValidateTokenAsync(token);
        if (company != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, CompanyGroupPrefix + company.Id);
            await base.OnConnectedAsync();
            return;
        }

        // Widget visitor token
        var branchId = await _sessionService.ValidateTokenAsync(token);
        if (branchId.HasValue)
        {
            Context.Items["BranchId"] = branchId.Value;
            await base.OnConnectedAsync();
            return;
        }

        throw new HubException("Invalid or expired token.");
    }

    public async Task SendMessage(Guid branchId, string? sessionId, string message)
    {
        if (Context.Items["BranchId"] is not Guid tokenBranchId || tokenBranchId != branchId)
        {
            throw new HubException("Branch does not match session.");
        }

        var aiWidget = await _aiWidgetRepository.GetByBranchIdAsync(branchId);
        if (aiWidget == null || !aiWidget.IsActive)
        {
            throw new HubException("AI Widget is not available for this branch.");
        }

        var content = message?.Trim() ?? "";
        if (string.IsNullOrEmpty(content))
        {
            throw new HubException("Message cannot be empty.");
        }

        var correlationId = Guid.NewGuid().ToString("N");
        _jobEnqueuer.EnqueueChat(aiWidget.TenantId, content, sessionId, Context.ConnectionId, correlationId);

        await Clients.Caller.SendAsync("MessageReceived", correlationId);
    }
}
