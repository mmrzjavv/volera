using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Administration.Commands;
using Core.Application.Administration.Queries;
using Core.Application.Administration.DTOs;
using Core.Application.Interfaces;
using WebAPI.Extensions;

namespace WebAPI.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = "Admin")]
public class AdminUserController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminUserController> _logger;
    private readonly ISessionService _sessionService;

    public AdminUserController(IMediator mediator, ILogger<AdminUserController> logger, ISessionService sessionService)
    {
        _mediator = mediator;
        _logger = logger;
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? roleFilter = null,
        [FromQuery] bool? isDisabled = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetAdminUserListQuery(page, pageSize, searchTerm, roleFilter, isDisabled, sortBy, sortDesc));
        return this.Success(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var result = await _mediator.Send(new GetAdminUserDetailQuery(id));
        return result != null ? this.Success(result) : this.ApiNotFound("User not found.");
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AdminUpdateUserRequest request)
    {
        var adminId = this.GetCurrentUserId();
        if (!adminId.HasValue) return this.ApiUnauthorized();
        await _mediator.Send(new AdminUpdateUserCommand(id, request.FirstName, request.LastName, request.Email, request.Bio, adminId.Value));
        return this.Success();
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id)
    {
        var adminId = this.GetCurrentUserId();
        if (!adminId.HasValue) return this.ApiUnauthorized();
        await _mediator.Send(new DisableUserCommand(id, adminId.Value));
        return this.Success();
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendUserRequest request)
    {
        var adminId = this.GetCurrentUserId();
        if (!adminId.HasValue) return this.ApiUnauthorized();
        await _mediator.Send(new SuspendUserCommand(id, request.Until, adminId.Value));
        return this.Success();
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var adminId = this.GetCurrentUserId();
        if (!adminId.HasValue) return this.ApiUnauthorized();
        await _mediator.Send(new ReactivateUserCommand(id, adminId.Value));
        return this.Success();
    }

    [HttpPost("{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, [FromBody] SetRoleRequest? request)
    {
        var adminId = this.GetCurrentUserId();
        if (!adminId.HasValue) return this.ApiUnauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Role))
            return this.Fail("Role is required.");
        var roleName = new[] { "User", "Moderator", "Admin", "SuperAdmin" }
            .FirstOrDefault(r => string.Equals(r, request.Role.Trim(), StringComparison.OrdinalIgnoreCase)) ?? request.Role.Trim();
        await _mediator.Send(new SetUserRoleCommand(id, roleName, adminId.Value));
        return this.Success();
    }

    [HttpGet("{id:guid}/sessions")]
    public async Task<IActionResult> GetSessions(Guid id, CancellationToken cancellationToken)
    {
        var sessions = await _sessionService.GetActiveSessionsForUserAsync(id, excludeSessionId: null, cancellationToken);
        return this.Success(sessions);
    }

    [HttpDelete("{id:guid}/sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid id, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);
        if (session == null)
            return this.ApiNotFound("Session not found.");
        if (session.UserId != id)
            return this.ApiNotFound("Session not found.");
        await _sessionService.RevokeSessionAsync(sessionId, cancellationToken);
        return this.Success();
    }
}

public record AdminUpdateUserRequest(string FirstName, string LastName, string? Email, string? Bio);
public record SuspendUserRequest(DateTime Until);
public record SetRoleRequest(string Role);
