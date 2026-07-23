using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Application.Interfaces;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly ISessionService _sessionService;

    public SessionController(ICurrentUserService currentUser, ISessionService sessionService)
    {
        _currentUser = currentUser;
        _sessionService = sessionService;
    }

    /// <summary>
    /// Lists active sessions for the current user (for "You are logged in from…"). Excludes the current session.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMySessions(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId == null)
            return this.ApiUnauthorized();

        var sessions = await _sessionService.GetActiveSessionsForUserAsync(userId.Value, _currentUser.SessionId, cancellationToken);
        return this.Success(sessions);
    }

    /// <summary>
    /// Revokes a session by id. Only the session owner can revoke. Returns 404 if session not found or not owned.
    /// </summary>
    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId == null)
            return this.ApiUnauthorized();

        var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);
        if (session == null || session.UserId != userId.Value)
            return this.ApiNotFound("Session not found.");

        await _sessionService.RevokeSessionAsync(sessionId, cancellationToken);
        return this.Success();
    }
}
