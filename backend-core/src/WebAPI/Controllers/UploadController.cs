using Core.Application.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Extensions;
using WebAPI.Models;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[EnableRateLimiting("AuthenticatedUploads")]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;

    public UploadController(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (!_fileStorageService.IsConfigured)
            return StatusCode(503, ApiResponse<object?>.Fail(NullFileStorageService.UnavailableMessage));

        if (file == null || file.Length == 0)
            return this.Fail("No file uploaded.");

        try
        {
            MediaContentValidator.ValidateOrThrow(file.FileName, file.ContentType, file.Length);
            using var stream = file.OpenReadStream();
            var objectKey = await _fileStorageService.UploadFileAsync(stream, file.FileName, file.ContentType);
            var downloadUrl = _fileStorageService.GetPresignedDownloadUrl(objectKey);
            // publicUrl kept for older clients; prefer storing objectKey server-side.
            return this.Success(new { url = downloadUrl, publicUrl = downloadUrl, objectKey });
        }
        catch (InvalidOperationException ex)
        {
            return this.Fail(ex.Message);
        }
            catch (Exception ex)
        {
            return new ObjectResult(ApiResponse<object?>.Fail($"Upload failed: {ex.Message}")) { StatusCode = 500 };
        }
    }

    [HttpPost("initiate")]
    public IActionResult InitiateUpload([FromBody] InitiateUploadRequest request)
    {
        if (!_fileStorageService.IsConfigured)
            return StatusCode(503, ApiResponse<object?>.Fail(NullFileStorageService.UnavailableMessage));

        if (string.IsNullOrEmpty(request.FileName) || string.IsNullOrEmpty(request.ContentType))
            return this.Fail("FileName and ContentType are required.");

        try
        {
            MediaContentValidator.ValidateOrThrow(request.FileName, request.ContentType);
            var (uploadUrl, objectKey) = _fileStorageService.GetPresignedUploadUrl(request.FileName, request.ContentType);
            var downloadUrl = _fileStorageService.GetPresignedDownloadUrl(objectKey);
            return this.Success(new
            {
                uploadUrl,
                objectKey,
                downloadUrl,
                publicUrl = objectKey // durable key for message AttachmentUrl; clients resolve via API
            });
        }
        catch (InvalidOperationException ex)
        {
            return this.Fail(ex.Message);
        }
        catch (Exception)
        {
            return new ObjectResult(ApiResponse<object?>.Fail("An error occurred while initiating upload.")) { StatusCode = 500 };
        }
    }

    /// <summary>Authorized short-lived download URL for a private object key owned/used by the caller.</summary>
    [HttpGet("download-url")]
    public IActionResult GetDownloadUrl([FromQuery] string objectKey)
    {
        if (!_fileStorageService.IsConfigured)
            return StatusCode(503, ApiResponse<object?>.Fail(NullFileStorageService.UnavailableMessage));

        if (string.IsNullOrWhiteSpace(objectKey))
            return this.Fail("objectKey is required.");

        try
        {
            var url = _fileStorageService.GetPresignedDownloadUrl(objectKey);
            return this.Success(new { url });
        }
        catch (InvalidOperationException ex)
        {
            return this.Fail(ex.Message);
        }
    }

    public class InitiateUploadRequest
    {
        public required string FileName { get; set; }
        public required string ContentType { get; set; }
    }
}
