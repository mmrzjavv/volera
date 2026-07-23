using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
using Core.Application.Queries;
using Core.Application.Interfaces;
using Core.Domain.Interfaces;
using WebAPI.Extensions;
using WebAPI.Models;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/company/ai-widget")]
public class CompanyAiWidgetController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICompanyTokenService _companyTokenService;
    private readonly ICompanyAiWidgetRepository _aiWidgetRepository;
    private readonly ICompanyWidgetRepository _widgetRepository;
    private readonly IAiWidgetSessionService _sessionService;
    private readonly ILogger<CompanyAiWidgetController> _logger;

    public CompanyAiWidgetController(
        IMediator mediator,
        ICompanyTokenService companyTokenService,
        ICompanyAiWidgetRepository aiWidgetRepository,
        ICompanyWidgetRepository widgetRepository,
        IAiWidgetSessionService sessionService,
        ILogger<CompanyAiWidgetController> logger)
    {
        _mediator = mediator;
        _companyTokenService = companyTokenService;
        _aiWidgetRepository = aiWidgetRepository;
        _widgetRepository = widgetRepository;
        _sessionService = sessionService;
        _logger = logger;
    }

    private async Task<Guid?> GetCompanyIdAsync(CancellationToken cancellationToken)
    {
        var token = Request.Headers[CompanyController.CompanyTokenHeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(token)) return null;
        var company = await _companyTokenService.ValidateTokenAsync(token, cancellationToken);
        return company?.Id;
    }

    /// <summary>Setup AI widget for a branch. Creates CompanyAiWidget if not exists. Requires company token.</summary>
    [HttpPost("setup")]
    public async Task<IActionResult> Setup([FromBody] SetupAiWidgetRequest request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");

        var command = new SetupCompanyAiWidgetCommand
        {
            CompanyId = companyId.Value,
            BranchId = request.BranchId
        };
        var result = await _mediator.Send(command, cancellationToken);
        return new ObjectResult(ApiResponse<object>.Ok(new
        {
            aiWidgetId = result.AiWidgetId,
            tenantId = result.TenantId
        })) { StatusCode = 201 };
    }

    /// <summary>Submit company text content for RAG indexing. Enqueues job and returns 202. Requires company token.</summary>
    [HttpPost("content")]
    public async Task<IActionResult> SubmitContent([FromBody] SubmitContentRequest request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");

        var command = new SubmitCompanyContentCommand
        {
            CompanyId = companyId.Value,
            BranchId = request.BranchId,
            Content = request.Content ?? ""
        };
        var result = await _mediator.Send(command, cancellationToken);
        return new ObjectResult(ApiResponse<object>.Ok(new
        {
            jobId = result.JobId,
            contentBlockId = result.ContentBlockId,
            status = "Processing"
        })) { StatusCode = 202 };
    }

    /// <summary>List company content blocks for a branch. Requires company token.</summary>
    [HttpGet("content")]
    public async Task<IActionResult> GetContent([FromQuery] Guid branchId, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");

        var query = new GetCompanyContentQuery
        {
            CompanyId = companyId.Value,
            BranchId = branchId
        };
        var list = await _mediator.Send(query, cancellationToken);
        return this.Success(list);
    }

    /// <summary>Create a session for the AI widget (visitor). Public; returns token for SignalR connection. Accepts branchId or widgetId (resolves widget to branch).</summary>
    [AllowAnonymous]
    [HttpPost("session")]
    public async Task<IActionResult> CreateSession([FromBody] CreateAiWidgetSessionRequest request, CancellationToken cancellationToken)
    {
        Guid branchId;
        if (!string.IsNullOrWhiteSpace(request.WidgetId))
        {
            var companyWidget = await _widgetRepository.GetByWidgetIdAsync(request.WidgetId!.Trim(), cancellationToken);
            if (companyWidget == null)
                return this.ApiNotFound("No active chat widget found for this widget ID. Use the branch that has both chat and AI widget set up.");
            branchId = companyWidget.BranchId;
        }
        else if (request.BranchId != Guid.Empty)
        {
            branchId = request.BranchId;
        }
        else
        {
            return this.Fail("Either branchId or widgetId is required.");
        }

        var aiWidget = await _aiWidgetRepository.GetByBranchIdAsync(branchId);
        if (aiWidget == null || !aiWidget.IsActive)
            return this.ApiNotFound("No active AI widget for this branch. Set up and index content in Admin → AI Widget for this branch.");

        var token = await _sessionService.CreateSessionAsync(branchId, cancellationToken);
        return new ObjectResult(ApiResponse<object>.Ok(new
        {
            token,
            expiresIn = 86400,
            branchId
        })) { StatusCode = 201 };
    }

    /// <summary>List AI widgets for the company (branches with AI widget set up). Requires company token.</summary>
    [HttpGet("list")]
    public async Task<IActionResult> ListWidgets(CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");

        var widgets = await _aiWidgetRepository.GetByCompanyIdAsync(companyId.Value, cancellationToken);
        var list = widgets.Select(w => new
        {
            branchId = w.BranchId,
            branchName = w.Branch?.Name ?? w.BranchId.ToString(),
            isActive = w.IsActive
        }).ToList();
        return this.Success(list);
    }

    /// <summary>Get embed script URL and params for the AI widget. Requires company token. Returns 404 if no widget for branch.</summary>
    [HttpGet("embed-info")]
    public async Task<IActionResult> GetEmbedInfo([FromQuery] Guid branchId, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");

        var widget = await _aiWidgetRepository.GetByBranchIdAsync(branchId, cancellationToken);
        if (widget == null || widget.CompanyId != companyId.Value)
            return this.ApiNotFound("No AI widget set up for this branch.");

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var scriptUrl = $"{baseUrl.TrimEnd('/')}/ai-widget.js";
        return this.Success(new
        {
            scriptUrl,
            branchId,
            dataBranch = branchId.ToString(),
            dataColor = "0d9488",
            dataPosition = "bottom-right",
            isActive = widget.IsActive
        });
    }
}

public class SetupAiWidgetRequest
{
    public Guid BranchId { get; set; }
}

public class SubmitContentRequest
{
    public Guid BranchId { get; set; }
    public string? Content { get; set; }
}

public class CreateAiWidgetSessionRequest
{
    public Guid BranchId { get; set; }
    /// <summary>Optional. When set, resolves to branch (same branch as chat widget with this ID).</summary>
    public string? WidgetId { get; set; }
}
