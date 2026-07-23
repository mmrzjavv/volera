using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Administration.Commands;
using Core.Application.Administration.Queries;
using WebAPI.Extensions;

namespace WebAPI.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/limits")]
[Authorize(Policy = "Admin")]
public class AdminLimitsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminLimitsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("system")]
    public async Task<IActionResult> GetSystemLimits()
    {
        var result = await _mediator.Send(new GetSystemLimitsQuery());
        return this.Success(result);
    }

    [HttpPut("system")]
    public async Task<IActionResult> SetSystemLimit([FromBody] SetSystemLimitRequest request)
    {
        var adminId = this.GetCurrentUserId();
        if (!adminId.HasValue) return this.ApiUnauthorized();
        await _mediator.Send(new SetSystemLimitCommand(request.Key, request.Value, adminId.Value));
        return this.Success();
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUserOverrides(Guid userId)
    {
        var result = await _mediator.Send(new GetUserLimitOverridesQuery(userId));
        return this.Success(result);
    }

    [HttpGet("users/{userId:guid}/effective")]
    public async Task<IActionResult> GetEffectiveLimits(Guid userId)
    {
        var result = await _mediator.Send(new GetEffectiveLimitsQuery(userId));
        return this.Success(result);
    }

    [HttpPut("users/{userId:guid}")]
    public async Task<IActionResult> SetUserOverride(Guid userId, [FromBody] SetUserLimitOverrideRequest request)
    {
        var adminId = this.GetCurrentUserId();
        if (!adminId.HasValue) return this.ApiUnauthorized();
        await _mediator.Send(new SetUserLimitOverrideCommand(userId, request.Key, request.Value, adminId.Value));
        return this.Success();
    }

    [HttpDelete("users/{userId:guid}/overrides")]
    public async Task<IActionResult> RemoveUserOverride(Guid userId, [FromQuery] string key)
    {
        var adminId = this.GetCurrentUserId();
        if (!adminId.HasValue) return this.ApiUnauthorized();
        await _mediator.Send(new RemoveUserLimitOverrideCommand(userId, key, adminId.Value));
        return this.Success();
    }
}

public record SetSystemLimitRequest(string Key, decimal Value);
public record SetUserLimitOverrideRequest(string Key, decimal Value);
