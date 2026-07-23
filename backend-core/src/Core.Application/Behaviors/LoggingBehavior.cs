using System.Diagnostics;
using Core.Application.Commands;
using Core.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs the lifecycle of each request/handler,
/// including the current user id and execution time, to create a clear story
/// of what happened in the system. For guest flows (no JWT), logs sender/guest context when available.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId;

        // For guest flows (no JWT), show SendMessageCommand SenderId or "Guest" for SendGuestMessageCommand so logs are clear
        var userLabel = userId.HasValue ? userId.ToString() : (request switch
        {
            SendMessageCommand sm => $"Guest/Sender {sm.SenderId}",
            SendGuestMessageCommand => "Guest",
            _ => "(null)"
        });

        var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");

        _logger.LogInformation(
            "Handling {RequestName} for User {UserLabel} with ActivityId {ActivityId} and Payload {@Request}",
            requestName,
            userLabel,
            activityId,
            request);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestName} for User {UserLabel} in {ElapsedMilliseconds} ms with ActivityId {ActivityId}",
                requestName,
                userLabel,
                stopwatch.ElapsedMilliseconds,
                activityId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Error handling {RequestName} for User {UserLabel} after {ElapsedMilliseconds} ms with ActivityId {ActivityId}",
                requestName,
                userLabel,
                stopwatch.ElapsedMilliseconds,
                activityId);

            throw;
        }
    }
}

