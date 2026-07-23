using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Application.Interfaces;
using WebAPI.Extensions;
using WebAPI.Models;

namespace WebAPI.Controllers;

[Authorize(AuthenticationSchemes = "SupportUser")]
[ApiController]
[Route("api/v1/support")]
public class SupportUploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;

    public SupportUploadController(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    /// <summary>Upload a file as a support user. Returns URL to use in reply AttachmentUrl.</summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return this.Fail("No file uploaded.");
        using var stream = file.OpenReadStream();
        try
        {
            var objectKey = await _fileStorageService.UploadFileAsync(stream, file.FileName, file.ContentType, "support");
            var url = _fileStorageService.ResolveClientUrl(objectKey) ?? objectKey;
            return this.Success(new { url, objectKey });
        }
        catch (Exception)
        {
            return new ObjectResult(ApiResponse<object?>.Fail("Upload failed.")) { StatusCode = 500 };
        }
    }
}
