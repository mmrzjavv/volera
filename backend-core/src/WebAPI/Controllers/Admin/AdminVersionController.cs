using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services;
using WebAPI.Extensions;

namespace WebAPI.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/version")]
[Authorize(Policy = "Admin")]
public class AdminVersionController : ControllerBase
{
    private readonly IAppVersionService _versionService;

    public AdminVersionController(IAppVersionService versionService)
    {
        _versionService = versionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetVersion()
    {
        var version = await _versionService.GetVersionAsync();
        return this.Success(new { version });
    }

    [HttpPut]
    public async Task<IActionResult> SetVersion([FromBody] SetVersionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Version))
            return this.BadRequest("Version is required.");
        await _versionService.SetVersionAsync(request.Version.Trim());
        return this.Success(new { version = request.Version.Trim() });
    }
}

public record SetVersionRequest([property: JsonPropertyName("version")] string Version);
