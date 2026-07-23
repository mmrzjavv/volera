using System.Security.Claims;
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

        _logger.LogError(
            "Frontend error [{Category}] {Message}. Url={Url} UserId={UserId} UserAgent={UserAgent} StackTrace={StackTrace} ComponentStack={ComponentStack}",
            category,
            request.Message.Trim(),
            request.Url?.Trim(),
            userId,
            request.UserAgent?.Trim(),
            request.StackTrace?.Trim(),
            request.ComponentStack?.Trim());

        return NoContent();
    }
}
