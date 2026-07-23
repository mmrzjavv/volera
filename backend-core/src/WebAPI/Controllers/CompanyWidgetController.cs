using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
using Core.Application.Queries;
using Core.Application.Interfaces;
using WebAPI.Extensions;
using WebAPI.Models;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/company/widget")]
public class CompanyWidgetController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICompanyTokenService _companyTokenService;
    private readonly ICompanyWidgetTokenService _widgetTokenService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<CompanyWidgetController> _logger;

    public const string CompanyClientTokenHeaderName = "X-Company-Client-Token";

    public CompanyWidgetController(IMediator mediator, ICompanyTokenService companyTokenService, ICompanyWidgetTokenService widgetTokenService, IFileStorageService fileStorageService, ILogger<CompanyWidgetController> logger)
    {
        _mediator = mediator;
        _companyTokenService = companyTokenService;
        _widgetTokenService = widgetTokenService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    private async Task<Guid?> GetCompanyIdAsync(CancellationToken cancellationToken)
    {
        var token = Request.Headers[CompanyController.CompanyTokenHeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(token)) return null;
        var company = await _companyTokenService.ValidateTokenAsync(token, cancellationToken);
        return company?.Id;
    }

    /// <summary>Generate a widget for a branch. Requires company token.</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateWidget([FromBody] GenerateWidgetRequest request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var command = new GenerateCompanyWidgetCommand
        {
            CompanyId = companyId.Value,
            BranchId = request.BranchId
        };
        var result = await _mediator.Send(command, cancellationToken);
        return new ObjectResult(ApiResponse<object>.Ok(new
        {
            widgetEntityId = result.WidgetEntityId,
            widgetId = result.WidgetId,
            widgetToken = result.WidgetToken
        })) { StatusCode = 201 };
    }

    /// <summary>Resolve branch ID to widget ID for embed. Public.</summary>
    [AllowAnonymous]
    [HttpGet("by-branch/{branchId:guid}")]
    public async Task<IActionResult> GetWidgetIdByBranch(Guid branchId, CancellationToken cancellationToken)
    {
        var query = new GetWidgetIdByBranchQuery { BranchId = branchId };
        var widgetId = await _mediator.Send(query, cancellationToken);
        if (string.IsNullOrEmpty(widgetId))
            return this.ApiNotFound("No widget found for this branch.");
        return this.Success(new { widgetId });
    }

    /// <summary>List widgets for the company. Requires company token.</summary>
    [HttpGet("list")]
    public async Task<IActionResult> ListWidgets(CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var query = new GetCompanyWidgetsQuery { CompanyId = companyId.Value };
        var widgets = await _mediator.Send(query, cancellationToken);
        return this.Success(widgets);
    }

    /// <summary>Create a client session for the widget. Public; widgetId in body.</summary>
    [AllowAnonymous]
    [HttpPost("client/session")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("CompanyWidgetClientSession")]
    public async Task<IActionResult> CreateClientSession([FromBody] CreateCompanyClientSessionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCompanyClientSessionCommand
        {
            WidgetId = request.WidgetId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Mobile = request.Mobile
        };
        var result = await _mediator.Send(command, cancellationToken);
        if (result == null)
            return this.ApiNotFound("Widget not found or inactive.");
        return new ObjectResult(ApiResponse<object>.Ok(new
        {
            clientToken = result.ClientToken,
            clientId = result.ClientId,
            expiresAt = result.ExpiresAt
        })) { StatusCode = 201 };
    }

    /// <summary>Send a message as a company widget client. Public; requires X-Company-Client-Token header.</summary>
    [AllowAnonymous]
    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendCompanyWidgetMessageRequest request, CancellationToken cancellationToken)
    {
        var token = Request.Headers[CompanyClientTokenHeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
            return this.ApiUnauthorized("Company client token is required. Send X-Company-Client-Token header.");
        var command = new SendCompanyMessageCommand
        {
            ClientToken = token,
            Content = request.Content ?? "",
            AttachmentUrl = request.AttachmentUrl,
            AttachmentType = request.AttachmentType,
            ReplyToMessageId = request.ReplyToMessageId
        };
        var messageId = await _mediator.Send(command, cancellationToken);
        return this.Success(new { messageId });
    }

    /// <summary>Upload a file as a company widget client. Returns URL to use in message AttachmentUrl. Public; requires X-Company-Client-Token header.</summary>
    [AllowAnonymous]
    [HttpPost("client/upload")]
    public async Task<IActionResult> ClientUpload(IFormFile file, CancellationToken cancellationToken = default)
    {
        var token = Request.Headers[CompanyClientTokenHeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
            return this.ApiUnauthorized("Company client token is required. Send X-Company-Client-Token header.");
        var client = await _widgetTokenService.ValidateCompanyClientTokenAsync(token, cancellationToken);
        if (client == null)
            return this.ApiUnauthorized("Invalid or expired company client token.");
        if (file == null || file.Length == 0)
            return this.Fail("No file uploaded.");
        using var stream = file.OpenReadStream();
        try
        {
            var objectKey = await _fileStorageService.UploadFileAsync(stream, file.FileName, file.ContentType, "widget");
            var url = _fileStorageService.ResolveClientUrl(objectKey) ?? objectKey;
            return this.Success(new { url, objectKey });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Widget client upload failed.");
            return new ObjectResult(ApiResponse<object>.Fail("Upload failed.")) { StatusCode = 500 };
        }
    }

    /// <summary>Get message history for the current widget client. Public; requires X-Company-Client-Token header.</summary>
    [AllowAnonymous]
    [HttpGet("messages")]
    public async Task<IActionResult> GetClientMessages([FromQuery] int limit = 50, [FromQuery] DateTime? before = null, CancellationToken cancellationToken = default)
    {
        var token = Request.Headers[CompanyClientTokenHeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
            return this.ApiUnauthorized("Company client token is required. Send X-Company-Client-Token header.");
        var client = await _widgetTokenService.ValidateCompanyClientTokenAsync(token, cancellationToken);
        if (client == null)
            return this.ApiUnauthorized("Invalid or expired company client token.");
        var query = new GetCompanyClientMessagesQuery
        {
            ClientUserId = client.UserId,
            BranchId = client.BranchId,
            Limit = Math.Clamp(limit, 1, 200),
            Before = before
        };
        var messages = await _mediator.Send(query, cancellationToken);
        if (messages == null)
            return this.Success(Array.Empty<object>());
        return this.Success(messages);
    }

    /// <summary>Delete a widget. Requires company token.</summary>
    [HttpDelete("{widgetId:guid}")]
    public async Task<IActionResult> DeleteWidget(Guid widgetId, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        if (companyId == null) return this.ApiUnauthorized("Valid company token required. Send X-Company-Token header.");
        var command = new DeleteCompanyWidgetCommand
        {
            WidgetId = widgetId,
            CompanyId = companyId.Value
        };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }
}

public class GenerateWidgetRequest
{
    public Guid BranchId { get; set; }
}

public class CreateCompanyClientSessionRequest
{
    public string WidgetId { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
}

public class SendCompanyWidgetMessageRequest
{
    public string? Content { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}
