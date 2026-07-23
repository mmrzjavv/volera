using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Administration.Commands;
using WebAPI.Extensions;

namespace WebAPI.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/groups")]
[Authorize(Policy = "Admin")]
public class AdminGroupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminGroupsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{groupId:guid}/profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(Guid groupId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return this.Fail("No file uploaded.");

        using var stream = file.OpenReadStream();
        var command = new UploadGroupProfilePictureCommand
        {
            GroupId = groupId,
            FileStream = stream,
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream"
        };

        try
        {
            var url = await _mediator.Send(command);
            return this.Success(new { url });
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Group not found.");
        }
    }
}
