using System.Security.Claims;
using Core.Application.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/errors")]
public class ErrorController : ControllerBase
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous]
    public IActionResult Report([FromBody] ReportErrorRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("userId");

        var category = string.IsNullOrWhiteSpace(request.Category) ? "React" : request.Category.Trim();

        AppLog.Error(_logger, AppLogEvents.UnhandledException, null,
            "Source: Frontend | Category: {Category} | UserId: {UserId} | Url: {Url} | Message: {ErrorMessage} | Result: Failure",
            category,
            userId,
            request.Url?.Trim(),
            Truncate(request.Message.Trim(), 500));

        return NoContent();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
