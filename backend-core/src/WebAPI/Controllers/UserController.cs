using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
using Core.Application.Queries;
using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileStorageService _fileStorage;

    public UserController(IMediator mediator, IFileStorageService fileStorage)
    {
        _mediator = mediator;
        _fileStorage = fileStorage;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var query = new GetUserByIdQuery { UserId = userId };
        var user = await _mediator.Send(query);
        if (user == null) return this.ApiNotFound("User not found");
        user.ProfilePicture = _fileStorage.ResolveClientUrl(user.ProfilePicture);
        return this.Success(user);
    }

    /// <summary>
    /// Public profile by user id for chat views (safe fields only).
    /// </summary>
    [HttpGet("{userId:guid}/profile")]
    public async Task<IActionResult> GetPublicProfile(Guid userId)
    {
        var query = new GetUserByIdQuery { UserId = userId };
        var user = await _mediator.Send(query);
        if (user == null) return this.ApiNotFound("User not found");

        var publicProfile = new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Username,
            ProfilePicture = _fileStorage.ResolveClientUrl(user.ProfilePicture),
            user.Bio
        };

        return this.Success(publicProfile);
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? term = null)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var query = new GetUsersQuery 
        { 
            ExcludeUserId = userId,
            Page = page,
            PageSize = pageSize,
            Term = term
        };
        var users = await _mediator.Send(query);
        return this.Success(users);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new UpdateProfileCommand
        {
            UserId = userId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            ProfilePicture = dto.ProfilePicture,
            Email = dto.Email,
            Bio = dto.Bio
        };

        await _mediator.Send(command);
        return this.Success();
    }

    [HttpPost("upload-profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return this.Fail("No file uploaded.");

        using var stream = file.OpenReadStream();
        var command = new UploadProfilePictureCommand
        {
            FileStream = stream,
            FileName = file.FileName,
            ContentType = file.ContentType
        };

        var objectKey = await _mediator.Send(command);
        var url = _fileStorage.ResolveClientUrl(objectKey) ?? objectKey;
        return this.Success(new { url, objectKey });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = dto.CurrentPassword,
            NewPassword = dto.NewPassword
        };

        await _mediator.Send(command);
        return this.Success();
    }

    [HttpDelete("offline")]
    public async Task<IActionResult> DeleteOfflineUsers()
    {
        var command = new DeleteOfflineUsersCommand();
        var deletedCount = await _mediator.Send(command);
        return this.Success(new { message = $"Deleted {deletedCount} offline user(s).", deletedCount });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? phoneNumber, [FromQuery] string? username)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) && string.IsNullOrWhiteSpace(username))
            return this.Fail("Provide phoneNumber or username.");

        UserDto? user;
        if (!string.IsNullOrWhiteSpace(username))
        {
            user = await _mediator.Send(new SearchUserByUsernameQuery { Username = username });
        }
        else
        {
            user = await _mediator.Send(new SearchUserByPhoneNumberQuery { PhoneNumber = phoneNumber! });
        }

        if (user == null) return this.ApiNotFound("User not found");
        return this.Success(user);
    }

    [HttpGet("message-length-limit")]
    public async Task<IActionResult> GetMessageLengthLimit([FromServices] ILimitResolutionService limitResolutionService)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var limit = await limitResolutionService.GetEffectiveLimitAsync(userId, LimitKeys.MaxMessageLength);
        return this.Success(new { limit = (int)limit });
    }
}