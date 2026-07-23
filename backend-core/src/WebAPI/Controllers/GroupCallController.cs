using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Core.Application.Commands;
using Core.Application.DTOs;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class GroupCallController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IGroupCallRepository _groupCallRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupCallNotificationService _groupCallNotificationService;

    public GroupCallController(
        IMediator mediator,
        IGroupCallRepository groupCallRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IGroupCallNotificationService groupCallNotificationService)
    {
        _mediator = mediator;
        _groupCallRepository = groupCallRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _groupCallNotificationService = groupCallNotificationService;
    }

    /// <summary>
    /// Initiate (or reuse) a group call for a given group.
    /// Returns the active groupCallId that the client should join.
    /// </summary>
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiateGroupCallDto dto)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var initiatorId))
        {
            return this.ApiUnauthorized();
        }

        var command = new InitiateGroupCallCommand
        {
            GroupId = dto.GroupId,
            InitiatorId = initiatorId,
            IsVideo = dto.IsVideo
        };

        var groupCallId = await _mediator.Send(command);
        return this.Success(new { groupCallId });
    }

    /// <summary>
    /// Join an existing group call as a participant.
    /// </summary>
    [HttpPost("{groupCallId:guid}/join")]
    public async Task<IActionResult> Join(Guid groupCallId)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return this.ApiUnauthorized();
        }

        var groupCall = await _groupCallRepository.GetByIdWithParticipantsAsync(groupCallId);

        // Make join idempotent and tolerant of races: if the call no longer exists or already ended,
        // we simply return success so the client can continue without surfacing errors.
        if (groupCall is null || groupCall.Status == Core.Domain.Entities.GroupCallStatus.Ended)
        {
            return this.Success();
        }

        // Add (or re-use) participant
        groupCall.AddParticipant(userId);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Treat concurrent modifications as a no-op from the client's perspective.
            return this.Success();
        }

        // Notify other participants that someone joined
        var user = await _userRepository.GetByIdAsync(userId);
        var userName = user != null
            ? $"{user.FirstName} {user.LastName}".Trim()
            : string.Empty;

        await _groupCallNotificationService.SendParticipantJoined(groupCallId, userId, userName);

        return this.Success();
    }

    /// <summary>
    /// End a group call (typically by the initiator, but any participant can trigger it).
    /// </summary>
    [HttpPost("{groupCallId:guid}/end")]
    public async Task<IActionResult> End(Guid groupCallId)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return this.ApiUnauthorized();
        }

        var groupCall = await _groupCallRepository.GetByIdWithParticipantsAsync(groupCallId);
        if (groupCall is null)
        {
            return this.NotFound("Group call not found.");
        }

        groupCall.End(userId);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // If the call was already ended/removed, treat as not found instead of throwing 500
            return this.NotFound("Group call not found or already ended.");
        }

        await _groupCallNotificationService.SendGroupCallEnded(groupCallId);

        return this.Success();
    }
}

