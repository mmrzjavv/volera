using System.Net;
using System.Text.Json;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebAPI.Models;

namespace WebAPI.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? context.User.FindFirst("userId")?.Value;

            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["UserId"] = userId,
                ["TraceIdentifier"] = context.TraceIdentifier
            }))
            {
                _logger.LogError(ex,
                    "An unhandled exception occurred while processing {RequestPath} {RequestMethod}. TraceIdentifier: {TraceIdentifier}",
                    context.Request.Path,
                    context.Request.Method,
                    context.TraceIdentifier);
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.ContentType = "application/json";
        int statusCode;
        IReadOnlyList<string>? messages;

        if (exception is ValidationException validationException)
        {
            statusCode = (int)HttpStatusCode.BadRequest;
            messages = validationException.Errors.Select(e => e.ErrorMessage).ToList();
        }
        else
        {
            statusCode = exception switch
            {
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                Core.Application.Exceptions.MaxSessionsReachedException => (int)HttpStatusCode.Conflict,
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };
            messages = new[] { exception.Message };
        }

        context.Response.StatusCode = statusCode;
        var response = messages != null && messages.Count > 0
            ? ApiResponse<object?>.Fail(messages)
            : ApiResponse<object?>.Fail(exception.Message);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
