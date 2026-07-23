using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models;

namespace WebAPI.Extensions;

public static class ControllerBaseExtensions
{
    /// <summary>
    /// Returns 200 OK with standardized envelope: { success: true, operationDate, data }.
    /// </summary>
    public static IActionResult Success<T>(this ControllerBase controller, T? data) =>
        controller.Ok(ApiResponse<T>.Ok(data));

    /// <summary>
    /// Returns 200 OK with no payload: { success: true, operationDate, data: null }.
    /// </summary>
    public static IActionResult Success(this ControllerBase controller) =>
        controller.Ok(ApiResponse<object?>.Ok(null));

    /// <summary>
    /// Returns 201 Created with standardized envelope: { success: true, operationDate, data }.
    /// </summary>
    public static IActionResult SuccessCreated<T>(this ControllerBase controller, string actionName, object? routeValues, T? data) =>
        controller.CreatedAtAction(actionName, routeValues, ApiResponse<T>.Ok(data));

    /// <summary>
    /// Returns 400 Bad Request with envelope: { success: false, operationDate, data: null, message }.
    /// </summary>
    public static IActionResult Fail(this ControllerBase controller, string? message = null) =>
        controller.BadRequest(ApiResponse<object?>.Fail(message));

    /// <summary>
    /// Returns 400 Bad Request with envelope and multiple messages (e.g. validation errors).
    /// </summary>
    public static IActionResult Fail(this ControllerBase controller, IReadOnlyList<string> messages) =>
        controller.BadRequest(ApiResponse<object?>.Fail(messages));

    /// <summary>
    /// Returns 404 Not Found with envelope: { success: false, operationDate, data: null, message }.
    /// </summary>
    public static IActionResult ApiNotFound(this ControllerBase controller, string? message = null) =>
        new ObjectResult(ApiResponse<object?>.Fail(message ?? "Resource not found")) { StatusCode = 404 };

    /// <summary>
    /// Returns 401 Unauthorized with envelope: { success: false, operationDate, data: null, message }.
    /// </summary>
    public static IActionResult ApiUnauthorized(this ControllerBase controller, string? message = null) =>
        new ObjectResult(ApiResponse<object?>.Fail(message ?? "Unauthorized")) { StatusCode = 401 };

    /// <summary>
    /// Returns 403 Forbidden with envelope: { success: false, operationDate, data: null, message }.
    /// </summary>
    public static IActionResult ApiForbid(this ControllerBase controller, string? message = null) =>
        new ObjectResult(ApiResponse<object?>.Fail(message ?? "Forbidden")) { StatusCode = 403 };

    /// <summary>
    /// Tries to get the current authenticated user's id from claims.
    /// Prefers ClaimTypes.NameIdentifier (set in JWT events) and falls back to the raw "userId" claim.
    /// Returns null when no valid Guid can be parsed.
    /// </summary>
    public static Guid? GetCurrentUserId(this ControllerBase controller)
    {
        var user = controller.User;
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            return null;
        }

        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? user.FindFirst("userId")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
