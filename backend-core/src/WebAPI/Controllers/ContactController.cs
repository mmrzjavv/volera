using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
using Core.Application.Queries;
using Core.Application.DTOs;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddContact([FromBody] AddContactDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new AddContactCommand
        {
            OwnerUserId = userId,
            ContactIdentifier = dto.ContactIdentifier,
            ContactName = dto.ContactName
        };
        var contactId = await _mediator.Send(command);
        return this.SuccessCreated(nameof(GetContacts), new { id = contactId }, new { id = contactId });
    }

    [HttpGet]
    public async Task<IActionResult> GetContacts([FromQuery] string? status)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var query = new GetContactsQuery { UserId = userId, Status = status };
        var contacts = await _mediator.Send(query);
        return this.Success(contacts);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContact(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new DeleteContactCommand { ContactId = id, UserId = userId };
        await _mediator.Send(command);
        return this.Success();
    }
    
    [HttpPost("sync")]
    public async Task<IActionResult> SyncContacts([FromBody] SyncContactsDto dto)
    {
         var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
         var command = new SyncContactsCommand { UserId = userId, PhoneNumbers = dto.PhoneNumbers };
         var contacts = await _mediator.Send(command);
         return this.Success(contacts);
    }
}
