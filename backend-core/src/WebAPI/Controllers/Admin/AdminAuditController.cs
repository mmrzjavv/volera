using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Administration.Queries;
using WebAPI.Extensions;

namespace WebAPI.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/audit")]
[Authorize(Policy = "Admin")]
public class AdminAuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? adminUserId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetAdminAuditLogQuery(page, pageSize, adminUserId, action, resourceType, from, to));
        return this.Success(result);
    }
}
