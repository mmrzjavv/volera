using Microsoft.Extensions.Logging;

namespace Core.Application.Logging;

/// <summary>
/// Helpers for consistent structured application logs.
/// Message templates always include {EventName} as the first property for Seq queries.
/// </summary>
public static class AppLog
{
    public static void Info(
        ILogger logger,
        EventId eventId,
        string messageTemplate,
        params object?[] args)
    {
        logger.LogInformation(eventId, PrependEventName(messageTemplate), PrependArgs(eventId, args));
    }

    public static void Warning(
        ILogger logger,
        EventId eventId,
        string messageTemplate,
        params object?[] args)
    {
        logger.LogWarning(eventId, PrependEventName(messageTemplate), PrependArgs(eventId, args));
    }

    public static void Warning(
        ILogger logger,
        EventId eventId,
        Exception? exception,
        string messageTemplate,
        params object?[] args)
    {
        logger.LogWarning(eventId, exception, PrependEventName(messageTemplate), PrependArgs(eventId, args));
    }

    public static void Error(
        ILogger logger,
        EventId eventId,
        Exception? exception,
        string messageTemplate,
        params object?[] args)
    {
        logger.LogError(eventId, exception, PrependEventName(messageTemplate), PrependArgs(eventId, args));
    }

    private static string PrependEventName(string messageTemplate) =>
        "{EventName} | " + messageTemplate;

    private static object?[] PrependArgs(EventId eventId, object?[] args)
    {
        var result = new object?[args.Length + 1];
        result[0] = eventId.Name ?? eventId.Id.ToString();
        Array.Copy(args, 0, result, 1, args.Length);
        return result;
    }
}
