using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
using Core.Application.Queries;
using Core.Domain.Entities;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[Authorize(AuthenticationSchemes = "SupportUser")]
[ApiController]
[Route("api/v1/support/users")]
public class SupportUserController : ControllerBase
{
    private readonly IMediator _mediator;

    public SupportUserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid? GetCompanyId()
    {
        var claim = User.FindFirst("companyId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private Guid? GetSupportUserId()
    {
        var claim = User.FindFirst("supportUserId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId();
        if (companyId == null) return this.ApiUnauthorized();
        var query = new GetSupportUsersByCompanyQuery { CompanyId = companyId.Value };
        var users = await _mediator.Send(query, cancellationToken);
        return this.Success(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupportUserRequest request, CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId();
        if (companyId == null) return this.ApiUnauthorized();
        var command = new CreateSupportUserCommand
        {
            CompanyId = companyId.Value,
            Username = request.Username,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };
        var id = await _mediator.Send(command, cancellationToken);
        return new ObjectResult(WebAPI.Models.ApiResponse<object>.Ok(new { supportUserId = id })) { StatusCode = 201 };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupportUserRequest request, CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId();
        if (companyId == null) return this.ApiUnauthorized();
        var command = new UpdateSupportUserCommand
        {
            SupportUserId = id,
            CompanyId = companyId.Value,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId();
        if (companyId == null) return this.ApiUnauthorized();
        var command = new DeleteSupportUserCommand { SupportUserId = id, CompanyId = companyId.Value };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    [HttpPost("{id:guid}/assign-branch")]
    public async Task<IActionResult> AssignBranch(Guid id, [FromBody] AssignBranchRequest request, CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId();
        if (companyId == null) return this.ApiUnauthorized();
        var command = new AssignSupportUserToBranchCommand
        {
            SupportUserId = id,
            BranchId = request.BranchId,
            CompanyId = companyId.Value
        };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    [HttpDelete("{id:guid}/assign-branch/{branchId:guid}")]
    public async Task<IActionResult> UnassignBranch(Guid id, Guid branchId, CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId();
        if (companyId == null) return this.ApiUnauthorized();
        var command = new UnassignSupportUserFromBranchCommand
        {
            SupportUserId = id,
            BranchId = branchId,
            CompanyId = companyId.Value
        };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    [HttpGet("{id:guid}/branches")]
    public async Task<IActionResult> GetBranches(Guid id, CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId();
        if (companyId == null) return this.ApiUnauthorized();
        var query = new GetSupportUserBranchesQuery { SupportUserId = id };
        var branches = await _mediator.Send(query, cancellationToken);
        return this.Success(branches);
    }

    [HttpGet("branches/{branchId:guid}/messages")]
    public async Task<IActionResult> GetBranchMessages(Guid branchId, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null, CancellationToken cancellationToken = default)
    {
        var supportUserId = GetSupportUserId();
        if (supportUserId == null) return this.ApiUnauthorized();
        var query = new GetSupportBranchMessagesQuery
        {
            SupportUserId = supportUserId.Value,
            BranchId = branchId,
            Limit = limit,
            Before = before
        };
        var messages = await _mediator.Send(query, cancellationToken);
        return this.Success(messages);
    }

    /// <summary>Send a reply as support to the branch. Optionally target a specific client (TargetClientUserId) so only they receive it in the widget.</summary>
    [HttpPost("branches/{branchId:guid}/messages")]
    public async Task<IActionResult> SendReply(Guid branchId, [FromBody] SendSupportReplyRequest request, CancellationToken cancellationToken = default)
    {
        var supportUserId = GetSupportUserId();
        if (supportUserId == null) return this.ApiUnauthorized();
        var command = new SendSupportReplyCommand
        {
            SupportUserId = supportUserId.Value,
            BranchId = branchId,
            TargetClientUserId = request.TargetClientUserId,
            Content = request.Content ?? "",
            AttachmentUrl = request.AttachmentUrl,
            AttachmentType = request.AttachmentType,
            ReplyToMessageId = request.ReplyToMessageId
        };
        var messageId = await _mediator.Send(command, cancellationToken);
        return new ObjectResult(WebAPI.Models.ApiResponse<object>.Ok(new { messageId })) { StatusCode = 201 };
    }

    [HttpPost("branches/{branchId:guid}/messages/{messageId:guid}/reaction")]
    public async Task<IActionResult> AddReaction(Guid branchId, Guid messageId, [FromBody] SupportReactionRequest request, CancellationToken cancellationToken = default)
    {
        var supportUserId = GetSupportUserId();
        if (supportUserId == null) return this.ApiUnauthorized();
        var command = new AddSupportReactionCommand
        {
            SupportUserId = supportUserId.Value,
            MessageId = messageId,
            Emoji = request?.Emoji ?? ""
        };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }

    [HttpDelete("branches/{branchId:guid}/messages/{messageId:guid}/reaction")]
    public async Task<IActionResult> RemoveReaction(Guid branchId, Guid messageId, CancellationToken cancellationToken = default)
    {
        var supportUserId = GetSupportUserId();
        if (supportUserId == null) return this.ApiUnauthorized();
        var command = new RemoveSupportReactionCommand
        {
            SupportUserId = supportUserId.Value,
            MessageId = messageId
        };
        await _mediator.Send(command, cancellationToken);
        return this.Success();
    }
}

public class CreateSupportUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public SupportUserRole Role { get; set; } = SupportUserRole.SupportAgent;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UpdateSupportUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class AssignBranchRequest
{
    public Guid BranchId { get; set; }
}

public class SendSupportReplyRequest
{
    public string? Content { get; set; }
    public Guid? TargetClientUserId { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}

public class SupportReactionRequest
{
    public string? Emoji { get; set; }
}
