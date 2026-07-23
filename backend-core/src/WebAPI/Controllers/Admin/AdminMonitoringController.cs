using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Administration.Queries;
using WebAPI.Extensions;

namespace WebAPI.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/monitoring")]
[Authorize(Policy = "Admin")]
public class AdminMonitoringController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminMonitoringController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _mediator.Send(new GetSystemStatsQuery());
        return this.Success(result);
    }

    [HttpGet("stats/extended")]
    public async Task<IActionResult> GetExtendedStats()
    {
        var result = await _mediator.Send(new GetExtendedMonitoringStatsQuery());
        return this.Success(result);
    }

    [HttpGet("over-limit")]
    public async Task<IActionResult> GetUsersOverLimit([FromQuery] string limitKey, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetUsersOverLimitQuery(limitKey, page, pageSize));
        return this.Success(result);
    }

    [HttpGet("messages-per-day")]
    public async Task<IActionResult> GetMessagesPerDay([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new GetMessagesPerDayQuery(days));
        return this.Success(result);
    }

    [HttpGet("most-active-users")]
    public async Task<IActionResult> GetMostActiveUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetMostActiveUsersQuery(page, pageSize));
        return this.Success(result);
    }

    [HttpGet("most-active-groups")]
    public async Task<IActionResult> GetMostActiveGroups([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetMostActiveGroupsQuery(page, pageSize));
        return this.Success(result);
    }

    [HttpGet("table-counts")]
    public async Task<IActionResult> GetTableRowCounts()
    {
        var result = await _mediator.Send(new GetTableRowCountsQuery());
        return this.Success(result);
    }

    [HttpGet("user-usage")]
    public async Task<IActionResult> GetUserUsage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = true)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetUserUsageQuery(page, pageSize, sortBy, sortDesc));
        return this.Success(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadMessagesCountQuery());
        return this.Success(new { count = result });
    }
}
