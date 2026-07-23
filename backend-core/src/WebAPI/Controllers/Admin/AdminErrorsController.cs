using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/errors")]
[Authorize(Policy = "Admin")]
public class AdminErrorsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AdminErrorsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Errors are ingested by Serilog → Seq. Returns the Seq UI URL for operators.
    /// </summary>
    [HttpGet]
    public IActionResult GetErrorLoggingInfo()
    {
        var seqUiUrl = _configuration["Seq:UiUrl"]
            ?? _configuration["SEQ_UI_URL"]
            ?? "http://localhost:5341";

        return this.Success(new
        {
            provider = "Seq",
            uiUrl = seqUiUrl,
            message = "Application errors are shipped to Seq via Serilog. Use the Seq UI to query and group logs."
        });
    }
}
