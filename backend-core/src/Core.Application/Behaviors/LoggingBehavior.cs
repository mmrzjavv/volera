using System.Diagnostics;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Application.Logging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Application.Behaviors;

/// <summary>
/// MediatR pipeline: no per-request success spam. Logs only failures for commands/queries
/// with structured context (never request payloads — they may contain passwords/tokens).
/// Business success events belong in handlers/controllers via <see cref="AppLogEvents"/>.
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
        var isCommand = requestName.EndsWith("Command", StringComparison.Ordinal);
        var userId = _currentUserService.UserId;
        var sessionId = _currentUserService.SessionId;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            // Duration-only at Debug for commands — invisible in production Information floor.
            if (isCommand)
            {
                _logger.LogDebug(
                    "CommandCompleted | RequestName: {RequestName} | UserId: {UserId} | SessionId: {SessionId} | ElapsedMs: {ElapsedMs}",
                    requestName,
                    userId,
                    sessionId,
                    stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Expected domain/auth failures are logged as business events by controllers/handlers.
            if (ex is UnauthorizedAccessException
                or KeyNotFoundException
                or InvalidOperationException
                or FluentValidation.ValidationException
                or Exceptions.MaxSessionsReachedException)
            {
                _logger.LogDebug(
                    ex,
                    "RequestRejected | RequestName: {RequestName} | UserId: {UserId} | Error: {ErrorType} | ElapsedMs: {ElapsedMs}",
                    requestName,
                    userId,
                    ex.GetType().Name,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }

            AppLog.Error(
                _logger,
                AppLogEvents.RequestFailed,
                ex,
                "RequestName: {RequestName} | UserId: {UserId} | SessionId: {SessionId} | ElapsedMs: {ElapsedMs} | Error: {ErrorType}",
                requestName,
                userId,
                sessionId,
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name);

            throw;
        }
    }
}
