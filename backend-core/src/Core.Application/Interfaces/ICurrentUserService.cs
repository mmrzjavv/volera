namespace Core.Application.Interfaces;

/// <summary>
/// Provides information about the current authenticated user to the application layer.
/// Implemented in the WebAPI project using IHttpContextAccessor.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The authenticated user's id (from JWT claims), or null when unauthenticated.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// True when a user is authenticated for the current request.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The current session id from JWT (sessionId claim), or null when not present.
    /// </summary>
    Guid? SessionId { get; }
}

